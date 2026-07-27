using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Communications.Contracts;
using PropertyIntelligence.Modules.Communications.Domain;
using PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Communications.Infrastructure;

internal sealed class CommunicationEvidenceReader(CommunicationsDbContext dbContext)
    : ICommunicationEvidenceReader
{
    public async Task<CommunicationEvidence> FindAsync(
        Guid organizationId,
        Guid claimId,
        string communicationType,
        string requiredStatus,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CommunicationStatus>(requiredStatus, true, out var parsedStatus))
        {
            return new CommunicationEvidence(
                CommunicationEvidenceOutcome.Unavailable,
                Detail: $"Communication status '{requiredStatus}' is not supported.");
        }

        var communication = await dbContext.Communications
            .AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == organizationId &&
                candidate.ClaimId == claimId &&
                candidate.CommunicationType == communicationType &&
                candidate.Status == parsedStatus)
            .OrderByDescending(candidate => candidate.ModifiedAt ?? candidate.CreatedAt)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return communication is null
            ? new CommunicationEvidence(
                CommunicationEvidenceOutcome.NotFound,
                Detail: $"No '{communicationType}' communication is {parsedStatus}.")
            : new CommunicationEvidence(
                CommunicationEvidenceOutcome.Found,
                communication.Id.ToString(),
                $"Communication '{communication.Id}' is {communication.Status}.");
    }
}
