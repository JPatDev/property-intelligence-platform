using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Api;

public sealed record CreateWorkflowRequest(
    WorkflowType Type,
    Guid SourcePlaybookId,
    Guid SourcePlaybookVersionId,
    int SnapshotSchemaVersion,
    IReadOnlyList<CreateWorkflowStageRequest> Stages)
{
    public WorkflowSnapshot ToSnapshot() =>
        new(
            SourcePlaybookId,
            SourcePlaybookVersionId,
            SnapshotSchemaVersion,
            Stages.Select(stage => stage.ToSnapshot()).ToArray());
}

public sealed record CreateWorkflowStageRequest(
    Guid SourceDefinitionId,
    string Name,
    int Order,
    bool IsOptional,
    IReadOnlyList<CreateWorkflowTaskRequest> Tasks)
{
    internal StageSnapshot ToSnapshot() =>
        new(
            SourceDefinitionId,
            Name,
            Order,
            IsOptional,
            Tasks.Select(task => task.ToSnapshot()).ToArray());
}

public sealed record CreateWorkflowTaskRequest(
    Guid SourceDefinitionId,
    string Name,
    int Order,
    int Priority,
    bool IsRequired,
    IReadOnlyCollection<Guid> DependencySourceDefinitionIds,
    IReadOnlyList<CreateCompletionGateRequest> CompletionGates,
    DateTimeOffset? DueAt = null)
{
    internal TaskSnapshot ToSnapshot() =>
        new(
            SourceDefinitionId,
            Name,
            Order,
            Priority,
            IsRequired,
            DependencySourceDefinitionIds.ToHashSet(),
            CompletionGates.Select(gate => gate.ToDefinition()).ToArray(),
            DueAt);
}

public sealed record CreateCompletionGateRequest(
    Guid Id,
    string GateType,
    CompletionGateScope Scope,
    CompletionGateSeverity Severity,
    IReadOnlyDictionary<string, string> Parameters,
    string FailureCode,
    string FailureMessage,
    int EvaluationVersion)
{
    internal CompletionGateDefinition ToDefinition() =>
        new(
            Id,
            GateType,
            Scope,
            Severity,
            Parameters,
            FailureCode,
            FailureMessage,
            EvaluationVersion);
}

public sealed record VersionedWorkflowRequest(long ExpectedVersion);

public sealed record AssignTaskRequest(Guid AssigneeId, long ExpectedVersion);

public sealed record CompleteTaskRequest(Guid ActorId, long ExpectedVersion);

public sealed record AddTaskBlockerRequest(
    string Code,
    string Description,
    Guid ActorId,
    long ExpectedVersion);

public sealed record ResolveTaskBlockerRequest(
    Guid ActorId,
    string Reason,
    long ExpectedVersion);

public sealed record SkipStageRequest(
    Guid ActorId,
    string Reason,
    long ExpectedVersion);

public sealed record CancelWorkflowRequest(
    Guid ActorId,
    string Reason,
    long ExpectedVersion);

public sealed record AcknowledgeEscalationRequest(Guid ActorId);
