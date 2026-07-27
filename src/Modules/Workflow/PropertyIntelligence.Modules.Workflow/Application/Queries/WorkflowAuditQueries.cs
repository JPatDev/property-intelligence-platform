using MediatR;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowAuditQuery(
    Guid OrganizationId,
    Guid WorkflowId,
    int Limit = 100) : IRequest<IReadOnlyList<WorkflowAuditDetails>>;

public sealed record WorkflowAuditDetails(
    Guid Id,
    Guid WorkflowId,
    string ActorType,
    Guid? ActorId,
    string Action,
    string? PreviousState,
    string? NewState,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string CausationId,
    bool SystemGenerated);
