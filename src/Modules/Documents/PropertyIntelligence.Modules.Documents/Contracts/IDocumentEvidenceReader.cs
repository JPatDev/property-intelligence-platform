namespace PropertyIntelligence.Modules.Documents.Contracts;

public enum DocumentEvidenceOutcome
{
    Found = 1,
    NotFound = 2,
    Unavailable = 3,
}

public sealed record DocumentEvidence(
    DocumentEvidenceOutcome Outcome,
    string? DocumentId = null,
    string? Detail = null);

public interface IDocumentEvidenceReader
{
    Task<DocumentEvidence> FindAsync(
        Guid organizationId,
        Guid claimId,
        string documentType,
        IReadOnlyCollection<string> acceptedStatuses,
        CancellationToken cancellationToken);
}
