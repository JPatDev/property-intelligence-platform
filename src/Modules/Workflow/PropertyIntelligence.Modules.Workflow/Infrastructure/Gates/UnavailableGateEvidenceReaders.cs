using PropertyIntelligence.Modules.Workflow.Application.Gates;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Gates;

internal sealed class UnavailableClaimGateEvidenceReader : IClaimGateEvidenceReader
{
    public Task<GateEvidence> HasFieldValueAsync(
        Guid organizationId,
        Guid claimId,
        string fieldName,
        CancellationToken cancellationToken) =>
        Task.FromResult(GateEvidence.Unavailable("Claims evidence integration is not configured."));
}

internal sealed class UnavailableDocumentGateEvidenceReader : IDocumentGateEvidenceReader
{
    public Task<GateEvidence> DocumentExistsAsync(
        Guid organizationId,
        Guid claimId,
        string documentType,
        IReadOnlyCollection<string> acceptedStatuses,
        CancellationToken cancellationToken) =>
        Task.FromResult(GateEvidence.Unavailable("Documents evidence integration is not configured."));
}

internal sealed class UnavailableCommunicationGateEvidenceReader : ICommunicationGateEvidenceReader
{
    public Task<GateEvidence> CommunicationExistsAsync(
        Guid organizationId,
        Guid claimId,
        string communicationType,
        string requiredStatus,
        CancellationToken cancellationToken) =>
        Task.FromResult(GateEvidence.Unavailable("Communications evidence integration is not configured."));
}

internal sealed class UnavailableApprovalGateEvidenceReader : IApprovalGateEvidenceReader
{
    public Task<GateEvidence> ApprovalExistsAsync(
        Guid organizationId,
        Guid claimId,
        string approvalType,
        CancellationToken cancellationToken) =>
        Task.FromResult(GateEvidence.Unavailable("Approval evidence integration is not configured."));
}

internal sealed class UnavailableRuleGateEvidenceReader : IRuleGateEvidenceReader
{
    public Task<GateEvidence> EvaluateRuleAsync(
        Guid organizationId,
        Guid claimId,
        string ruleCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken) =>
        Task.FromResult(GateEvidence.Unavailable("Business-rule evidence integration is not configured."));
}
