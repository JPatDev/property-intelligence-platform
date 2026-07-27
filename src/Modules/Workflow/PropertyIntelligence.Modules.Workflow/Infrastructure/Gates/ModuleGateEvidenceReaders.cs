using PropertyIntelligence.Modules.Claims.Contracts;
using PropertyIntelligence.Modules.Communications.Contracts;
using PropertyIntelligence.Modules.Documents.Contracts;
using PropertyIntelligence.Modules.Workflow.Application.Gates;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Gates;

internal sealed class ClaimsModuleGateEvidenceReader(IClaimFactsReader claims)
    : IClaimGateEvidenceReader
{
    public async Task<GateEvidence> HasFieldValueAsync(
        Guid organizationId,
        Guid claimId,
        string fieldName,
        CancellationToken cancellationToken)
    {
        var evidence = await claims.GetFieldEvidenceAsync(
            organizationId,
            claimId,
            fieldName,
            cancellationToken);

        return evidence.Outcome switch
        {
            ClaimFactEvidenceOutcome.Present =>
                GateEvidence.Satisfied(evidence.EvidenceReference, evidence.Detail),
            ClaimFactEvidenceOutcome.Missing => GateEvidence.Unsatisfied(evidence.Detail),
            _ => GateEvidence.Unavailable(evidence.Detail),
        };
    }
}

internal sealed class DocumentsModuleGateEvidenceReader(IDocumentEvidenceReader documents)
    : IDocumentGateEvidenceReader
{
    public async Task<GateEvidence> DocumentExistsAsync(
        Guid organizationId,
        Guid claimId,
        string documentType,
        IReadOnlyCollection<string> acceptedStatuses,
        CancellationToken cancellationToken)
    {
        var evidence = await documents.FindAsync(
            organizationId,
            claimId,
            documentType,
            acceptedStatuses,
            cancellationToken);

        return evidence.Outcome switch
        {
            DocumentEvidenceOutcome.Found =>
                GateEvidence.Satisfied(evidence.DocumentId, evidence.Detail),
            DocumentEvidenceOutcome.NotFound => GateEvidence.Unsatisfied(evidence.Detail),
            _ => GateEvidence.Unavailable(evidence.Detail),
        };
    }
}

internal sealed class CommunicationsModuleGateEvidenceReader(
    ICommunicationEvidenceReader communications) : ICommunicationGateEvidenceReader
{
    public async Task<GateEvidence> CommunicationExistsAsync(
        Guid organizationId,
        Guid claimId,
        string communicationType,
        string requiredStatus,
        CancellationToken cancellationToken)
    {
        var evidence = await communications.FindAsync(
            organizationId,
            claimId,
            communicationType,
            requiredStatus,
            cancellationToken);

        return evidence.Outcome switch
        {
            CommunicationEvidenceOutcome.Found =>
                GateEvidence.Satisfied(evidence.CommunicationId, evidence.Detail),
            CommunicationEvidenceOutcome.NotFound => GateEvidence.Unsatisfied(evidence.Detail),
            _ => GateEvidence.Unavailable(evidence.Detail),
        };
    }
}
