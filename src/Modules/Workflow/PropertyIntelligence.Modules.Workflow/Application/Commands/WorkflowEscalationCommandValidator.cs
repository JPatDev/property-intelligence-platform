using FluentValidation;

namespace PropertyIntelligence.Modules.Workflow.Application.Commands;

internal sealed class AcknowledgeWorkflowEscalationCommandValidator
    : AbstractValidator<AcknowledgeWorkflowEscalationCommand>
{
    public AcknowledgeWorkflowEscalationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.EscalationId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
    }
}
