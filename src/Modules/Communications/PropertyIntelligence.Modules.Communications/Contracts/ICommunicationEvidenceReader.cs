namespace PropertyIntelligence.Modules.Communications.Contracts;

public enum CommunicationEvidenceOutcome
{
    Found = 1,
    NotFound = 2,
    Unavailable = 3,
}

public sealed record CommunicationEvidence(
    CommunicationEvidenceOutcome Outcome,
    string? CommunicationId = null,
    string? Detail = null);

public interface ICommunicationEvidenceReader
{
    Task<CommunicationEvidence> FindAsync(
        Guid organizationId,
        Guid claimId,
        string communicationType,
        string requiredStatus,
        CancellationToken cancellationToken);
}
