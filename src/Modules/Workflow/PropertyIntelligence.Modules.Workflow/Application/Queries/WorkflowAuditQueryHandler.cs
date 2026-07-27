using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

internal sealed class GetWorkflowAuditQueryHandler(WorkflowDbContext dbContext)
    : IRequestHandler<GetWorkflowAuditQuery, IReadOnlyList<WorkflowAuditDetails>>
{
    public async Task<IReadOnlyList<WorkflowAuditDetails>> Handle(
        GetWorkflowAuditQuery request,
        CancellationToken cancellationToken) =>
        await dbContext.AuditRecords
            .AsNoTracking()
            .Where(audit =>
                audit.OrganizationId == request.OrganizationId &&
                audit.WorkflowId == request.WorkflowId)
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.Id)
            .Take(request.Limit)
            .Select(audit => new WorkflowAuditDetails(
                audit.Id,
                audit.WorkflowId,
                audit.ActorType,
                audit.ActorId,
                audit.Action,
                audit.PreviousState,
                audit.NewState,
                audit.OccurredAt,
                audit.CorrelationId,
                audit.CausationId,
                audit.SystemGenerated))
            .ToListAsync(cancellationToken);
}
