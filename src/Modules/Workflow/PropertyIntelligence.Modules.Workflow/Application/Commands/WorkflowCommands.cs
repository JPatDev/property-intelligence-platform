using MediatR;
using PropertyIntelligence.Modules.Workflow.Application.Abstractions;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Commands;

public sealed record CreateWorkflowCommand(
    Guid OrganizationId,
    Guid ClaimId,
    WorkflowType Type,
    WorkflowSnapshot Snapshot) : IWorkflowCommand<Guid>;

public sealed record StartWorkflowCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;

public sealed record AssignTaskCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid TaskId,
    Guid AssigneeId,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;

public sealed record StartTaskCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid TaskId,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;

public sealed record CompleteTaskCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid TaskId,
    Guid ActorId,
    long ExpectedVersion)
    : IExistingWorkflowCommand<TaskCompletionResult>;

public sealed record AddTaskBlockerCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid TaskId,
    string Code,
    string Description,
    Guid ActorId,
    long ExpectedVersion) : IExistingWorkflowCommand<Guid>;

public sealed record ResolveTaskBlockerCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid TaskId,
    Guid BlockerId,
    Guid ActorId,
    string Reason,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;

public sealed record SkipStageCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid StageId,
    Guid ActorId,
    string Reason,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;

public sealed record CancelWorkflowCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid ActorId,
    string Reason,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;

public sealed record ArchiveWorkflowCommand(
    Guid OrganizationId,
    Guid WorkflowId,
    long ExpectedVersion) : IExistingWorkflowCommand<Unit>;
