using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Gates;

public sealed record GateEvaluationContext(
    WorkflowInstance Workflow,
    WorkflowTask Task,
    CompletionGateDefinition Gate);

public interface ICompletionGateHandler
{
    string GateType { get; }

    Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken);
}

public interface ICompletionGateEvaluator
{
    Task<IReadOnlyList<GateEvaluationResult>> EvaluateAsync(
        WorkflowInstance workflow,
        WorkflowTask task,
        CancellationToken cancellationToken);
}

public enum GateEvidenceOutcome
{
    Satisfied = 1,
    Unsatisfied = 2,
    Unavailable = 3,
}

public sealed record GateEvidence(
    GateEvidenceOutcome Outcome,
    string? EvidenceReference = null,
    string? Detail = null)
{
    public static GateEvidence Satisfied(string? reference = null, string? detail = null) =>
        new(GateEvidenceOutcome.Satisfied, reference, detail);

    public static GateEvidence Unsatisfied(string? detail = null) =>
        new(GateEvidenceOutcome.Unsatisfied, Detail: detail);

    public static GateEvidence Unavailable(string? detail = null) =>
        new(GateEvidenceOutcome.Unavailable, Detail: detail);
}

public interface IClaimGateEvidenceReader
{
    Task<GateEvidence> HasFieldValueAsync(
        Guid organizationId,
        Guid claimId,
        string fieldName,
        CancellationToken cancellationToken);
}

public interface IDocumentGateEvidenceReader
{
    Task<GateEvidence> DocumentExistsAsync(
        Guid organizationId,
        Guid claimId,
        string documentType,
        IReadOnlyCollection<string> acceptedStatuses,
        CancellationToken cancellationToken);
}

public interface ICommunicationGateEvidenceReader
{
    Task<GateEvidence> CommunicationExistsAsync(
        Guid organizationId,
        Guid claimId,
        string communicationType,
        string requiredStatus,
        CancellationToken cancellationToken);
}

public interface IApprovalGateEvidenceReader
{
    Task<GateEvidence> ApprovalExistsAsync(
        Guid organizationId,
        Guid claimId,
        string approvalType,
        CancellationToken cancellationToken);
}

public interface IRuleGateEvidenceReader
{
    Task<GateEvidence> EvaluateRuleAsync(
        Guid organizationId,
        Guid claimId,
        string ruleCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken);
}
