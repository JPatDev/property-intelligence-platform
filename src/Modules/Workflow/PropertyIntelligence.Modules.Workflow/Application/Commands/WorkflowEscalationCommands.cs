using MediatR;
using PropertyIntelligence.Modules.Workflow.Application.Abstractions;

namespace PropertyIntelligence.Modules.Workflow.Application.Commands;

public sealed record AcknowledgeWorkflowEscalationCommand(
    Guid OrganizationId,
    Guid EscalationId,
    Guid ActorId) : IWorkflowCommand<Unit>;
