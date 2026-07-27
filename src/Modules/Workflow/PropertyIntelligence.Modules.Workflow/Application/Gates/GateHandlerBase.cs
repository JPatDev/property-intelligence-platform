using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Gates;

internal abstract class GateHandlerBase(TimeProvider timeProvider) : ICompletionGateHandler
{
    protected TimeProvider TimeProvider { get; } = timeProvider;

    public abstract string GateType { get; }

    public abstract Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken);

    protected GateEvaluationResult FromEvidence(Guid gateId, GateEvidence evidence) =>
        new(
            gateId,
            evidence.Outcome switch
            {
                GateEvidenceOutcome.Satisfied => GateEvaluationOutcome.Passed,
                GateEvidenceOutcome.Unsatisfied => GateEvaluationOutcome.Failed,
                _ => GateEvaluationOutcome.Indeterminate,
            },
            TimeProvider.GetUtcNow(),
            evidence.EvidenceReference,
            evidence.Detail);

    protected GateEvaluationResult Indeterminate(Guid gateId, string detail) =>
        new(
            gateId,
            GateEvaluationOutcome.Indeterminate,
            TimeProvider.GetUtcNow(),
            Detail: detail);

    protected static bool TryGetParameter(
        CompletionGateDefinition gate,
        string name,
        out string value)
    {
        if (gate.Parameters.TryGetValue(name, out var parameter) &&
            !string.IsNullOrWhiteSpace(parameter))
        {
            value = parameter.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }
}
