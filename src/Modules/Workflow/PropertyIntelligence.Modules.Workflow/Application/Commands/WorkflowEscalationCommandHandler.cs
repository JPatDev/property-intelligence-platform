using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Application.Errors;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Commands;

internal sealed class AcknowledgeWorkflowEscalationCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<AcknowledgeWorkflowEscalationCommand, Unit>
{
    public async Task<Unit> Handle(
        AcknowledgeWorkflowEscalationCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await dbContext.Escalations.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId &&
                candidate.Id == request.EscalationId,
            cancellationToken)
            ?? throw new WorkflowEscalationNotFoundException(request.EscalationId);

        escalation.Acknowledge(request.ActorId, timeProvider.GetUtcNow());
        return Unit.Value;
    }
}
