using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Documents.Contracts;
using PropertyIntelligence.Modules.Documents.Domain;
using PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Documents.Infrastructure;

internal sealed class DocumentEvidenceReader(DocumentsDbContext dbContext)
    : IDocumentEvidenceReader
{
    public async Task<DocumentEvidence> FindAsync(
        Guid organizationId,
        Guid claimId,
        string documentType,
        IReadOnlyCollection<string> acceptedStatuses,
        CancellationToken cancellationToken)
    {
        if (acceptedStatuses.Count == 0)
        {
            return new DocumentEvidence(
                DocumentEvidenceOutcome.Unavailable,
                Detail: "At least one accepted document status is required.");
        }

        var parsedStatuses = acceptedStatuses
            .Select(status => Enum.TryParse<DocumentStatus>(status, true, out var parsed)
                ? parsed
                : (DocumentStatus?)null)
            .Where(status => status.HasValue)
            .Select(status => status!.Value)
            .ToArray();
        if (parsedStatuses.Length == 0)
        {
            return new DocumentEvidence(
                DocumentEvidenceOutcome.Unavailable,
                Detail: "None of the accepted document statuses are supported.");
        }

        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == organizationId &&
                candidate.ClaimId == claimId &&
                candidate.DocumentType == documentType)
            .OrderByDescending(candidate => candidate.ModifiedAt ?? candidate.CreatedAt)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Status,
            })
            .FirstOrDefaultAsync(
                candidate => parsedStatuses.Contains(candidate.Status),
                cancellationToken);

        return document is null
            ? new DocumentEvidence(
                DocumentEvidenceOutcome.NotFound,
                Detail: $"No '{documentType}' document has an accepted status.")
            : new DocumentEvidence(
                DocumentEvidenceOutcome.Found,
                document.Id.ToString(),
                $"Document '{document.Id}' is {document.Status}.");
    }
}
