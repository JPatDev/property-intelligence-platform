using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

internal sealed class GetWorkflowNextActionQueryHandler(WorkflowDbContext dbContext)
    : IRequestHandler<GetWorkflowNextActionQuery, NextActionDetails?>
{
    public async Task<NextActionDetails?> Handle(
        GetWorkflowNextActionQuery request,
        CancellationToken cancellationToken) =>
        await dbContext.NextActions
            .AsNoTracking()
            .Where(action =>
                action.OrganizationId == request.OrganizationId &&
                action.WorkflowId == request.WorkflowId)
            .Select(action => new NextActionDetails(
                action.WorkflowId,
                action.TaskId,
                action.Action,
                action.OwnerId,
                action.DueAt,
                action.CalculatedPriority,
                action.ReasonCode,
                action.Reason,
                action.CalculatedAt,
                action.CalculationVersion))
            .SingleOrDefaultAsync(cancellationToken);
}

internal sealed class ListWorkflowEscalationsQueryHandler(WorkflowDbContext dbContext)
    : IRequestHandler<ListWorkflowEscalationsQuery, IReadOnlyList<WorkflowEscalationDetails>>
{
    public async Task<IReadOnlyList<WorkflowEscalationDetails>> Handle(
        ListWorkflowEscalationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Escalations
            .AsNoTracking()
            .Where(escalation => escalation.OrganizationId == request.OrganizationId);

        if (request.WorkflowId.HasValue)
        {
            query = query.Where(escalation => escalation.WorkflowId == request.WorkflowId);
        }

        if (!request.IncludeResolved)
        {
            query = query.Where(escalation =>
                escalation.Status != Domain.WorkflowEscalationStatus.Resolved);
        }

        return await query
            .OrderByDescending(escalation => escalation.Severity)
            .ThenBy(escalation => escalation.TriggeredAt)
            .Select(escalation => new WorkflowEscalationDetails(
                escalation.Id,
                escalation.WorkflowId,
                escalation.TaskId,
                escalation.Type,
                escalation.Severity,
                escalation.Status,
                escalation.Message,
                escalation.TriggeredAt,
                escalation.LastObservedAt,
                escalation.ResolvedAt))
            .ToListAsync(cancellationToken);
    }
}
