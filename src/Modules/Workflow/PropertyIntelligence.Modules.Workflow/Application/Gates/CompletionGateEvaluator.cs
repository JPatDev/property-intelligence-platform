using Microsoft.Extensions.Logging;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Gates;

internal sealed class CompletionGateEvaluator(
    IEnumerable<ICompletionGateHandler> handlers,
    TimeProvider timeProvider,
    ILogger<CompletionGateEvaluator> logger) : ICompletionGateEvaluator
{
    private readonly IReadOnlyDictionary<string, ICompletionGateHandler> _handlers =
        handlers.ToDictionary(handler => handler.GateType, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<GateEvaluationResult>> EvaluateAsync(
        WorkflowInstance workflow,
        WorkflowTask task,
        CancellationToken cancellationToken)
    {
        var results = new List<GateEvaluationResult>(task.CompletionGates.Count);

        foreach (var gate in task.CompletionGates)
        {
            if (!_handlers.TryGetValue(gate.GateType, out var handler))
            {
                results.Add(Indeterminate(
                    gate.Id,
                    $"No completion gate handler is registered for '{gate.GateType}'."));
                continue;
            }

            try
            {
                results.Add(await handler.EvaluateAsync(
                    new GateEvaluationContext(workflow, task, gate),
                    cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Completion gate {GateId} of type {GateType} could not be evaluated for workflow {WorkflowId}",
                    gate.Id,
                    gate.GateType,
                    workflow.Id);
                results.Add(Indeterminate(
                    gate.Id,
                    "The authoritative evidence source could not be evaluated."));
            }
        }

        return results;
    }

    private GateEvaluationResult Indeterminate(Guid gateId, string detail) =>
        new(
            gateId,
            GateEvaluationOutcome.Indeterminate,
            timeProvider.GetUtcNow(),
            Detail: detail);
}
