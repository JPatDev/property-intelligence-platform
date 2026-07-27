namespace PropertyIntelligence.Modules.Claims.Contracts;

public enum ClaimFactEvidenceOutcome
{
    Present = 1,
    Missing = 2,
    Unavailable = 3,
}

public sealed record ClaimFactEvidence(
    ClaimFactEvidenceOutcome Outcome,
    string? EvidenceReference = null,
    string? Detail = null);

public interface IClaimFactsReader
{
    Task<ClaimFactEvidence> GetFieldEvidenceAsync(
        Guid organizationId,
        Guid claimId,
        string fieldName,
        CancellationToken cancellationToken);
}
