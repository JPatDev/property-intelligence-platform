using FluentValidation;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

internal sealed class GetWorkflowAuditQueryValidator : AbstractValidator<GetWorkflowAuditQuery>
{
    public GetWorkflowAuditQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.WorkflowId).NotEmpty();
        RuleFor(query => query.Limit).InclusiveBetween(1, 500);
    }
}
