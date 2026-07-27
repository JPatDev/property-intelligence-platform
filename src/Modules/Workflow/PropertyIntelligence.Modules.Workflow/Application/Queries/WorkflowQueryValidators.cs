using FluentValidation;

namespace PropertyIntelligence.Modules.Workflow.Application.Queries;

internal sealed class GetWorkflowQueryValidator : AbstractValidator<GetWorkflowQuery>
{
    public GetWorkflowQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.WorkflowId).NotEmpty();
    }
}

internal sealed class ListClaimWorkflowsQueryValidator : AbstractValidator<ListClaimWorkflowsQuery>
{
    public ListClaimWorkflowsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.ClaimId).NotEmpty();
    }
}

internal sealed class GetTaskCompletionReadinessQueryValidator
    : AbstractValidator<GetTaskCompletionReadinessQuery>
{
    public GetTaskCompletionReadinessQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.WorkflowId).NotEmpty();
        RuleFor(query => query.TaskId).NotEmpty();
    }
}
