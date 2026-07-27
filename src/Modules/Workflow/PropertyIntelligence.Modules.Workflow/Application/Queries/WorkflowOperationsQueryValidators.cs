using FluentValidation;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

internal sealed class GetWorkflowNextActionQueryValidator
    : AbstractValidator<GetWorkflowNextActionQuery>
{
    public GetWorkflowNextActionQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.WorkflowId).NotEmpty();
    }
}

internal sealed class ListWorkflowEscalationsQueryValidator
    : AbstractValidator<ListWorkflowEscalationsQuery>
{
    public ListWorkflowEscalationsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.WorkflowId)
            .NotEqual(Guid.Empty)
            .When(query => query.WorkflowId.HasValue);
    }
}
