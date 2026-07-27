using MediatR;

namespace PropertyIntelligence.Modules.Workflow.Application.Abstractions;

public interface IWorkflowCommand<out TResponse> : IRequest<TResponse>;

public interface IExistingWorkflowCommand<out TResponse> : IWorkflowCommand<TResponse>
{
    Guid OrganizationId { get; }
    Guid WorkflowId { get; }
}
