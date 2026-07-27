using MediatR;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowNextActionQuery(Guid OrganizationId, Guid WorkflowId)
    : IRequest<NextActionDetails?>;

public sealed record ListWorkflowEscalationsQuery(
    Guid OrganizationId,
    Guid? WorkflowId = null,
    bool IncludeResolved = false) : IRequest<IReadOnlyList<WorkflowEscalationDetails>>;

public sealed record NextActionDetails(
    Guid WorkflowId,
    Guid TaskId,
    string Action,
    Guid? OwnerId,
    DateTimeOffset? DueAt,
    int CalculatedPriority,
    string ReasonCode,
    string Reason,
    DateTimeOffset CalculatedAt,
    int CalculationVersion);

public sealed record WorkflowEscalationDetails(
    Guid Id,
    Guid WorkflowId,
    Guid? TaskId,
    WorkflowEscalationType Type,
    WorkflowEscalationSeverity Severity,
    WorkflowEscalationStatus Status,
    string Message,
    DateTimeOffset TriggeredAt,
    DateTimeOffset LastObservedAt,
    DateTimeOffset? ResolvedAt);
