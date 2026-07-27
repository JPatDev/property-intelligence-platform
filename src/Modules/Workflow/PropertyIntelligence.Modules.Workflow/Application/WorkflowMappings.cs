using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application;

internal static class WorkflowMappings
{
    public static WorkflowDetails ToDetails(this WorkflowInstance workflow) =>
        new(
            workflow.Id,
            workflow.OrganizationId,
            workflow.ClaimId,
            workflow.Type,
            workflow.Status,
            workflow.Version,
            workflow.SourcePlaybookId,
            workflow.SourcePlaybookVersionId,
            workflow.CreatedAt,
            workflow.StartedAt,
            workflow.CompletedAt,
            workflow.Stages
                .OrderBy(stage => stage.Order)
                .Select(stage => new WorkflowStageDetails(
                    stage.Id,
                    stage.SourceDefinitionId,
                    stage.Name,
                    stage.Order,
                    stage.IsOptional,
                    stage.Status,
                    stage.Tasks
                        .OrderBy(task => task.Order)
                        .Select(task => new WorkflowTaskDetails(
                            task.Id,
                            task.SourceDefinitionId,
                            task.Name,
                            task.Order,
                            task.Priority,
                            task.IsRequired,
                            task.DueAt,
                            task.Status,
                            task.AssignedTo,
                            task.StartedAt,
                            task.CompletedAt,
                            task.DependencySourceDefinitionIds.ToArray(),
                            task.CompletionGates.Select(gate => new CompletionGateDetails(
                                gate.Id,
                                gate.SourceDefinitionId,
                                gate.GateType,
                                gate.Scope,
                                gate.Severity,
                                gate.FailureCode,
                                gate.FailureMessage,
                                gate.EvaluationVersion)).ToArray(),
                            task.Blockers.Select(blocker => new WorkflowBlockerDetails(
                                blocker.Id,
                                blocker.Code,
                                blocker.Description,
                                blocker.CreatedAt,
                                blocker.ResolvedAt,
                                blocker.IsResolved)).ToArray()))
                        .ToArray()))
                .ToArray());
}

public sealed record WorkflowDetails(
    Guid Id,
    Guid OrganizationId,
    Guid ClaimId,
    WorkflowType Type,
    WorkflowStatus Status,
    long Version,
    Guid SourcePlaybookId,
    Guid SourcePlaybookVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<WorkflowStageDetails> Stages);

public sealed record WorkflowStageDetails(
    Guid Id,
    Guid SourceDefinitionId,
    string Name,
    int Order,
    bool IsOptional,
    WorkflowStageStatus Status,
    IReadOnlyList<WorkflowTaskDetails> Tasks);

public sealed record WorkflowTaskDetails(
    Guid Id,
    Guid SourceDefinitionId,
    string Name,
    int Order,
    int Priority,
    bool IsRequired,
    DateTimeOffset? DueAt,
    WorkflowTaskStatus Status,
    Guid? AssignedTo,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<Guid> DependencySourceDefinitionIds,
    IReadOnlyList<CompletionGateDetails> CompletionGates,
    IReadOnlyList<WorkflowBlockerDetails> Blockers);

public sealed record CompletionGateDetails(
    Guid Id,
    Guid SourceDefinitionId,
    string GateType,
    CompletionGateScope Scope,
    CompletionGateSeverity Severity,
    string FailureCode,
    string FailureMessage,
    int EvaluationVersion);

public sealed record WorkflowBlockerDetails(
    Guid Id,
    string Code,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    bool IsResolved);
