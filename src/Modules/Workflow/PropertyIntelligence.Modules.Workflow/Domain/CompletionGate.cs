using System.Collections.ObjectModel;

namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class CompletionGateDefinition
{
    private CompletionGateDefinition()
    {
    }

    public CompletionGateDefinition(
        Guid id,
        string gateType,
        CompletionGateScope scope,
        CompletionGateSeverity severity,
        IReadOnlyDictionary<string, string> parameters,
        string failureCode,
        string failureMessage,
        int evaluationVersion)
    {
        Id = id;
        GateType = gateType;
        Scope = scope;
        Severity = severity;
        Parameters = parameters;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        EvaluationVersion = evaluationVersion;
    }

    public Guid Id { get; private set; }
    public Guid SourceDefinitionId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string GateType { get; private set; } = string.Empty;
    public CompletionGateScope Scope { get; private set; }
    public CompletionGateSeverity Severity { get; private set; }
    public IReadOnlyDictionary<string, string> Parameters { get; private set; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    public string FailureCode { get; private set; } = string.Empty;
    public string FailureMessage { get; private set; } = string.Empty;
    public int EvaluationVersion { get; private set; }

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

    internal CompletionGateDefinition Freeze(Guid organizationId)
    {
        var sourceDefinitionId = Id;
        var gate = new CompletionGateDefinition(
            Guid.NewGuid(),
            GateType.Trim(),
            Scope,
            Severity,
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(Parameters, StringComparer.Ordinal)),
            FailureCode.Trim(),
            FailureMessage.Trim(),
            EvaluationVersion);
        gate.OrganizationId = organizationId;
        gate.SourceDefinitionId = sourceDefinitionId;
        return gate;
    }
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
