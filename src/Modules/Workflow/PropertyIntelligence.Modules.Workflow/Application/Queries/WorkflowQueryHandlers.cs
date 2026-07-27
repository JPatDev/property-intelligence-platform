using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Application.Errors;
using PropertyIntelligence.Modules.Workflow.Application.Gates;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

internal sealed class GetWorkflowQueryHandler(WorkflowDbContext dbContext)
    : IRequestHandler<GetWorkflowQuery, WorkflowDetails>
{
    public async Task<WorkflowDetails> Handle(
        GetWorkflowQuery request,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId &&
                    candidate.Id == request.WorkflowId,
                cancellationToken)
            ?? throw new WorkflowNotFoundException(request.WorkflowId);

        return workflow.ToDetails();
    }
}

internal sealed class ListClaimWorkflowsQueryHandler(WorkflowDbContext dbContext)
    : IRequestHandler<ListClaimWorkflowsQuery, IReadOnlyList<WorkflowDetails>>
{
    public async Task<IReadOnlyList<WorkflowDetails>> Handle(
        ListClaimWorkflowsQuery request,
        CancellationToken cancellationToken)
    {
        var workflows = await dbContext.Workflows
            .AsNoTracking()
            .AsSplitQuery()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId &&
                candidate.ClaimId == request.ClaimId)
            .OrderBy(candidate => candidate.CreatedAt)
            .ToListAsync(cancellationToken);

        return workflows.Select(workflow => workflow.ToDetails()).ToArray();
    }
}

internal sealed class GetTaskCompletionReadinessQueryHandler(
    WorkflowDbContext dbContext,
    ICompletionGateEvaluator gateEvaluator)
    : IRequestHandler<GetTaskCompletionReadinessQuery, TaskCompletionReadiness>
{
    public async Task<TaskCompletionReadiness> Handle(
        GetTaskCompletionReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId &&
                    candidate.Id == request.WorkflowId,
                cancellationToken)
            ?? throw new WorkflowNotFoundException(request.WorkflowId);
        var task = workflow.GetTask(request.TaskId);
        var evaluations = await gateEvaluator.EvaluateAsync(
            workflow,
            task,
            cancellationToken);
        var requiredGateIds = task.CompletionGates
            .Where(gate => gate.Severity == Domain.CompletionGateSeverity.Required)
            .Select(gate => gate.Id)
            .ToHashSet();
        var passedRequiredGateIds = evaluations
            .Where(evaluation =>
                evaluation.Outcome == Domain.GateEvaluationOutcome.Passed &&
                requiredGateIds.Contains(evaluation.GateId))
            .Select(evaluation => evaluation.GateId)
            .ToHashSet();
        var blockingReasons = new List<string>();

        if (task.Status is not (
                Domain.WorkflowTaskStatus.Assigned or
                Domain.WorkflowTaskStatus.InProgress))
        {
            blockingReasons.Add($"Task status '{task.Status}' cannot be completed.");
        }

        if (task.HasOpenBlockers)
        {
            blockingReasons.Add("The task has one or more open blockers.");
        }

        var incompleteDependencies = task.DependencySourceDefinitionIds
            .Select(sourceDefinitionId => workflow.Stages
                .SelectMany(stage => stage.Tasks)
                .Single(dependency => dependency.SourceDefinitionId == sourceDefinitionId))
            .Where(dependency => dependency.Status != Domain.WorkflowTaskStatus.Completed)
            .ToArray();
        if (incompleteDependencies.Length > 0)
        {
            blockingReasons.Add("One or more task dependencies are incomplete.");
        }

        if (!requiredGateIds.SetEquals(passedRequiredGateIds))
        {
            blockingReasons.Add("One or more required completion gates did not pass.");
        }

        return new TaskCompletionReadiness(
            workflow.Id,
            task.Id,
            workflow.Version,
            blockingReasons.Count == 0,
            blockingReasons,
            evaluations);
    }
}
