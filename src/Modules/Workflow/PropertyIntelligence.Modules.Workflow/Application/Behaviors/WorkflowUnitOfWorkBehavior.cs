using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Application.Abstractions;
using PropertyIntelligence.Modules.Workflow.Application.Errors;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Behaviors;

internal sealed class WorkflowUnitOfWorkBehavior<TRequest, TResponse>(
    WorkflowDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IWorkflowCommand<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new WorkflowConcurrencyException(exception);
        }

        return response;
    }
}
