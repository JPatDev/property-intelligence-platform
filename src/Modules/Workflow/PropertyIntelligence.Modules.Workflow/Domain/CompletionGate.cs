using System.Collections.ObjectModel;

namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed record CompletionGateDefinition(
    Guid Id,
    string GateType,
    CompletionGateScope Scope,
    CompletionGateSeverity Severity,
    IReadOnlyDictionary<string, string> Parameters,
    string FailureCode,
    string FailureMessage,
    int EvaluationVersion)
{
    internal void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.gate_id_required", "A completion gate ID is required.");
        }

        if (string.IsNullOrWhiteSpace(GateType))
        {
            throw new WorkflowDomainException("workflow.gate_type_required", "A completion gate type is required.");
        }

        if (string.IsNullOrWhiteSpace(FailureCode) || string.IsNullOrWhiteSpace(FailureMessage))
        {
            throw new WorkflowDomainException(
                "workflow.gate_failure_details_required",
                "A completion gate requires a failure code and message.");
        }

        if (EvaluationVersion < 1)
        {
            throw new WorkflowDomainException(
                "workflow.gate_version_invalid",
                "A completion gate evaluation version must be at least one.");
        }
    }

    internal CompletionGateDefinition Freeze() => this with
    {
        Parameters = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(Parameters, StringComparer.Ordinal)),
    };
}

public sealed record GateEvaluationResult(
    Guid GateId,
    GateEvaluationOutcome Outcome,
    DateTimeOffset EvaluatedAt,
    string? EvidenceReference = null,
    string? Detail = null);

public sealed record CompletionFailure(string Code, string Message, GateEvaluationOutcome Outcome);

public sealed record TaskCompletionResult(bool Succeeded, IReadOnlyList<CompletionFailure> Failures)
{
    public static TaskCompletionResult Success { get; } = new(true, []);
}
