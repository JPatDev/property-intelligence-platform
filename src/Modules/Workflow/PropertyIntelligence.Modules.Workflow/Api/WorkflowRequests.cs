using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Api;

public sealed record CreateWorkflowRequest(
    WorkflowType Type,
    string DefinitionKey,
    int DefinitionVersion);

public sealed record VersionedWorkflowRequest(long ExpectedVersion);

public sealed record AssignTaskRequest(Guid AssigneeId, long ExpectedVersion);

public sealed record CompleteTaskRequest(long ExpectedVersion);

public sealed record AddTaskBlockerRequest(
    string Code,
    string Description,
    long ExpectedVersion);

public sealed record ResolveTaskBlockerRequest(
    string Reason,
    long ExpectedVersion);

public sealed record SkipStageRequest(
    string Reason,
    long ExpectedVersion);

public sealed record CancelWorkflowRequest(
    string Reason,
    long ExpectedVersion);
