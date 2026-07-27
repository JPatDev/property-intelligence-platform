using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropertyIntelligence.Modules.Workflow.Application.Auditing;
using PropertyIntelligence.Modules.Workflow.Application.Errors;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Operations;

public interface IWorkflowOperationalControlService
{
    Task RefreshAsync(
        Guid organizationId,
        Guid workflowId,
        bool systemGenerated,
        CancellationToken cancellationToken);
}

internal sealed class WorkflowOperationalControlService(
    WorkflowDbContext dbContext,
    INextActionCalculator nextActionCalculator,
    IOptions<WorkflowOperationsOptions> options,
    IWorkflowChangeRecorder changeRecorder,
    TimeProvider timeProvider) : IWorkflowOperationalControlService
{
    public async Task RefreshAsync(
        Guid organizationId,
        Guid workflowId,
        bool systemGenerated,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == organizationId &&
                    candidate.Id == workflowId,
                cancellationToken)
            ?? throw new WorkflowNotFoundException(workflowId);
        var now = timeProvider.GetUtcNow();
        var nextAction = nextActionCalculator.Calculate(workflow, now);

        await RefreshNextActionAsync(workflow, nextAction, now, cancellationToken);
        await RefreshEscalationsAsync(workflow, nextAction, now, cancellationToken);

        if (systemGenerated && dbContext.ChangeTracker.HasChanges())
        {
            var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            var state = JsonSerializer.Serialize(new
            {
                nextActionTaskId = nextAction?.TaskId,
                nextActionReasonCode = nextAction?.ReasonCode,
                openEscalationCount = dbContext.Escalations.Local.Count(escalation =>
                    escalation.WorkflowId == workflow.Id &&
                    escalation.Status != WorkflowEscalationStatus.Resolved),
            });
            changeRecorder.Record(
                workflow.OrganizationId,
                workflow.Id,
                "OperationalControlRefresh",
                null,
                true,
                null,
                state,
                [nameof(NextActionSnapshot), nameof(WorkflowEscalation)],
                workflow.Version,
                now,
                correlationId,
                "WorkflowOperationsWorker");
        }
    }

    private async Task RefreshNextActionAsync(
        WorkflowInstance workflow,
        CalculatedNextAction? calculated,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.NextActions.SingleOrDefaultAsync(
            action =>
                action.OrganizationId == workflow.OrganizationId &&
                action.WorkflowId == workflow.Id,
            cancellationToken);

        if (calculated is null)
        {
            if (existing is not null)
            {
                dbContext.NextActions.Remove(existing);
            }

            return;
        }

        if (existing is null)
        {
            dbContext.NextActions.Add(new NextActionSnapshot(
                workflow.Id,
                workflow.OrganizationId,
                calculated.TaskId,
                calculated.Action,
                calculated.OwnerId,
                calculated.DueAt,
                calculated.CalculatedPriority,
                calculated.ReasonCode,
                calculated.Reason,
                now,
                options.Value.CalculationVersion));
            return;
        }

        existing.Update(
            calculated.TaskId,
            calculated.Action,
            calculated.OwnerId,
            calculated.DueAt,
            calculated.CalculatedPriority,
            calculated.ReasonCode,
            calculated.Reason,
            now,
            options.Value.CalculationVersion);
    }

    private async Task RefreshEscalationsAsync(
        WorkflowInstance workflow,
        CalculatedNextAction? nextAction,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var activeConditions = BuildConditions(workflow, nextAction, now);
        var existing = await dbContext.Escalations
            .Where(escalation =>
                escalation.OrganizationId == workflow.OrganizationId &&
                escalation.WorkflowId == workflow.Id)
            .ToListAsync(cancellationToken);

        foreach (var condition in activeConditions)
        {
            var escalation = existing.SingleOrDefault(
                candidate => candidate.DeduplicationKey == condition.DeduplicationKey);
            if (escalation is null)
            {
                escalation = new WorkflowEscalation(
                    workflow.OrganizationId,
                    workflow.Id,
                    condition.TaskId,
                    condition.Type,
                    condition.Severity,
                    condition.DeduplicationKey,
                    condition.Message,
                    now);
                dbContext.Escalations.Add(escalation);
                existing.Add(escalation);
            }
            else
            {
                escalation.Observe(condition.Severity, condition.Message, now);
            }
        }

        var activeKeys = activeConditions.Select(condition => condition.DeduplicationKey).ToHashSet();
        foreach (var escalation in existing.Where(escalation =>
                     escalation.Status != WorkflowEscalationStatus.Resolved &&
                     !activeKeys.Contains(escalation.DeduplicationKey)))
        {
            escalation.Resolve(now, "The triggering workflow condition cleared.");
        }
    }

    private IReadOnlyList<EscalationCondition> BuildConditions(
        WorkflowInstance workflow,
        CalculatedNextAction? nextAction,
        DateTimeOffset now)
    {
        var conditions = new List<EscalationCondition>();
        if (workflow.Status is not (WorkflowStatus.Active or WorkflowStatus.Blocked))
        {
            return conditions;
        }

        var tasks = workflow.CurrentStages
            .SelectMany(stage => stage.Tasks)
            .Where(task => task.Status is not (
                WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled))
            .ToArray();

        foreach (var task in tasks)
        {
            if (task.DueAt < now)
            {
                var severity = now - task.DueAt >= options.Value.CriticalOverdueAge
                    ? WorkflowEscalationSeverity.Critical
                    : WorkflowEscalationSeverity.Warning;
                conditions.Add(Condition(
                    workflow,
                    task,
                    WorkflowEscalationType.TaskOverdue,
                    severity,
                    $"Task '{task.Name}' is overdue."));
            }

            if (!task.AssignedTo.HasValue &&
                task.Status != WorkflowTaskStatus.Blocked &&
                DependenciesComplete(workflow, task))
            {
                conditions.Add(Condition(
                    workflow,
                    task,
                    WorkflowEscalationType.TaskUnassigned,
                    WorkflowEscalationSeverity.Warning,
                    $"Task '{task.Name}' has no assigned owner."));
            }

            var oldestOpenBlocker = task.Blockers
                .Where(blocker => !blocker.IsResolved)
                .MinBy(blocker => blocker.CreatedAt);
            if (oldestOpenBlocker is not null &&
                now - oldestOpenBlocker.CreatedAt >= options.Value.BlockerEscalationAge)
            {
                var severity = now - oldestOpenBlocker.CreatedAt >=
                               options.Value.BlockerEscalationAge * 2
                    ? WorkflowEscalationSeverity.Critical
                    : WorkflowEscalationSeverity.Warning;
                conditions.Add(Condition(
                    workflow,
                    task,
                    WorkflowEscalationType.BlockerAged,
                    severity,
                    $"Task '{task.Name}' has been blocked too long."));
            }
        }

        if (nextAction is null)
        {
            conditions.Add(new EscalationCondition(
                WorkflowEscalationType.NoNextAction,
                WorkflowEscalationSeverity.Critical,
                null,
                $"{WorkflowEscalationType.NoNextAction}:{workflow.Id}",
                "The active workflow has no actionable next step."));
        }

        return conditions;
    }

    private static EscalationCondition Condition(
        WorkflowInstance workflow,
        WorkflowTask task,
        WorkflowEscalationType type,
        WorkflowEscalationSeverity severity,
        string message) =>
        new(
            type,
            severity,
            task.Id,
            $"{type}:{workflow.Id}:{task.Id}",
            message);

    private static bool DependenciesComplete(WorkflowInstance workflow, WorkflowTask task)
    {
        var completedSourceIds = workflow.Stages
            .SelectMany(stage => stage.Tasks)
            .Where(candidate => candidate.Status == WorkflowTaskStatus.Completed)
            .Select(candidate => candidate.SourceDefinitionId)
            .ToHashSet();
        return task.DependencySourceDefinitionIds.All(completedSourceIds.Contains);
    }

    private sealed record EscalationCondition(
        WorkflowEscalationType Type,
        WorkflowEscalationSeverity Severity,
        Guid? TaskId,
        string DeduplicationKey,
        string Message);
}
