using PropertyIntelligence.Modules.Workflow.Application.Gates;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Gates;

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
