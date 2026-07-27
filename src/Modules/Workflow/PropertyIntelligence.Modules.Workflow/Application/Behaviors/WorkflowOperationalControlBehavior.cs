using MediatR;
using PropertyIntelligence.Modules.Workflow.Application.Abstractions;
using PropertyIntelligence.Modules.Workflow.Application.Operations;

namespace PropertyIntelligence.Modules.Workflow.Application.Behaviors;

internal sealed class WorkflowOperationalControlBehavior<TRequest, TResponse>(
    IWorkflowOperationalControlService operationalControlService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IWorkflowCommand<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IExistingWorkflowCommand<TResponse> workflowCommand)
        {
            await operationalControlService.RefreshAsync(
                workflowCommand.OrganizationId,
                workflowCommand.WorkflowId,
                false,
                cancellationToken);
        }

        return response;
    }
}
