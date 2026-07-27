using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyIntelligence.Modules.Workflow.Application.Operations;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Operations;

internal sealed class WorkflowOperationsWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkflowOperationsOptions> options,
    ILogger<WorkflowOperationsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.ScanInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RefreshBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Periodic workflow operational-control refresh failed");
            }
        }
    }

    private async Task RefreshBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IWorkflowOperationalControlService>();
        var lastWorkflowId = Guid.Empty;

        while (!cancellationToken.IsCancellationRequested)
        {
            var workflowKeys = await dbContext.Workflows
                .AsNoTracking()
                .Where(workflow =>
                    workflow.Id.CompareTo(lastWorkflowId) > 0 &&
                    (workflow.Status == WorkflowStatus.Active ||
                     workflow.Status == WorkflowStatus.Blocked))
                .OrderBy(workflow => workflow.Id)
                .Select(workflow => new { workflow.OrganizationId, workflow.Id })
                .Take(options.Value.ScanBatchSize)
                .ToListAsync(cancellationToken);

            if (workflowKeys.Count == 0)
            {
                return;
            }

            foreach (var workflow in workflowKeys)
            {
                await service.RefreshAsync(
                    workflow.OrganizationId,
                    workflow.Id,
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            lastWorkflowId = workflowKeys[^1].Id;
        }
    }
}
