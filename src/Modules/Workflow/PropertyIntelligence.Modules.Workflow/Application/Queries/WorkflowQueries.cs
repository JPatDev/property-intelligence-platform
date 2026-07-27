using MediatR;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowQuery(Guid OrganizationId, Guid WorkflowId)
    : IRequest<WorkflowDetails>;

public sealed record ListClaimWorkflowsQuery(Guid OrganizationId, Guid ClaimId)
    : IRequest<IReadOnlyList<WorkflowDetails>>;

public sealed record GetTaskCompletionReadinessQuery(
    Guid OrganizationId,
    Guid WorkflowId,
    Guid TaskId) : IRequest<TaskCompletionReadiness>;

public sealed record TaskCompletionReadiness(
    Guid WorkflowId,
    Guid TaskId,
    long WorkflowVersion,
    bool CanComplete,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<GateEvaluationResult> Evaluations);
