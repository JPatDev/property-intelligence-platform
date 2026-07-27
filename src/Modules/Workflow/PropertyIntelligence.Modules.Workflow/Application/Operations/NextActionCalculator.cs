using Microsoft.Extensions.Options;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Operations;

public sealed record CalculatedNextAction(
    Guid TaskId,
    string Action,
    Guid? OwnerId,
    DateTimeOffset? DueAt,
    int CalculatedPriority,
    string ReasonCode,
    string Reason);

public interface INextActionCalculator
{
    CalculatedNextAction? Calculate(WorkflowInstance workflow, DateTimeOffset now);
}

internal sealed class NextActionCalculator(IOptions<WorkflowOperationsOptions> options)
    : INextActionCalculator
{
    public CalculatedNextAction? Calculate(WorkflowInstance workflow, DateTimeOffset now)
    {
        if (workflow.Status is not (WorkflowStatus.Active or WorkflowStatus.Blocked))
        {
            return null;
        }

        var candidates = workflow.CurrentStages
            .SelectMany(stage => stage.Tasks.Select(task => new { Stage = stage, Task = task }))
            .Where(candidate => candidate.Task.Status is not (
                WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled))
            .Where(candidate =>
                candidate.Task.Status == WorkflowTaskStatus.Blocked ||
                DependenciesComplete(workflow, candidate.Task))
            .Select(candidate => Rank(candidate.Stage, candidate.Task, now))
            .OrderBy(candidate => candidate.Category)
            .ThenByDescending(candidate => candidate.Task.Priority)
            .ThenBy(candidate => candidate.Task.DueAt ?? DateTimeOffset.MaxValue)
            .ThenBy(candidate => candidate.Stage.Order)
            .ThenBy(candidate => candidate.Task.Order)
            .ThenBy(candidate => candidate.Task.Id)
            .FirstOrDefault();

        if (candidates is null)
        {
            return null;
        }

        var score = ((6 - candidates.Category) * 100_000) + candidates.Task.Priority;
        return new CalculatedNextAction(
            candidates.Task.Id,
            candidates.Action,
            candidates.Task.AssignedTo,
            candidates.Task.DueAt,
            score,
            candidates.ReasonCode,
            candidates.Reason);
    }

    private RankedCandidate Rank(
        WorkflowStage stage,
        WorkflowTask task,
        DateTimeOffset now)
    {
        if (task.Status == WorkflowTaskStatus.Blocked)
        {
            var criticallyOverdue = task.DueAt < now &&
                                    now - task.DueAt >= options.Value.CriticalOverdueAge;
            return new RankedCandidate(
                stage,
                task,
                criticallyOverdue ? 1 : 3,
                $"Resolve blocker for: {task.Name}",
                criticallyOverdue ? "CRITICALLY_OVERDUE_TASK_BLOCKED" : "TASK_BLOCKED",
                criticallyOverdue
                    ? "The critically overdue task cannot proceed until its blocker is resolved."
                    : "The task cannot proceed until its blocker is resolved.");
        }

        if (task.DueAt < now)
        {
            var critical = now - task.DueAt >= options.Value.CriticalOverdueAge;
            return new RankedCandidate(
                stage,
                task,
                critical ? 1 : 2,
                $"Complete overdue task: {task.Name}",
                critical ? "TASK_CRITICALLY_OVERDUE" : "TASK_OVERDUE",
                critical
                    ? "The task is critically overdue."
                    : "The task is overdue.");
        }

        if (task.DueAt <= now + options.Value.DueSoonWindow)
        {
            return new RankedCandidate(
                stage,
                task,
                4,
                task.Name,
                "TASK_DUE_SOON",
                "The task is due soon.");
        }

        return new RankedCandidate(
            stage,
            task,
            5,
            task.Name,
            task.AssignedTo.HasValue ? "NEXT_ORDERED_TASK" : "TASK_UNASSIGNED",
            task.AssignedTo.HasValue
                ? "This is the highest-priority actionable task."
                : "This actionable task requires an owner.");
    }

    private static bool DependenciesComplete(WorkflowInstance workflow, WorkflowTask task)
    {
        var completedSourceIds = workflow.Stages
            .SelectMany(stage => stage.Tasks)
            .Where(candidate => candidate.Status == WorkflowTaskStatus.Completed)
            .Select(candidate => candidate.SourceDefinitionId)
            .ToHashSet();
        return task.DependencySourceDefinitionIds.All(completedSourceIds.Contains);
    }

    private sealed record RankedCandidate(
        WorkflowStage Stage,
        WorkflowTask Task,
        int Category,
        string Action,
        string ReasonCode,
        string Reason);
}
