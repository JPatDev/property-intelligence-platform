using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Claims.Contracts;
using PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Claims.Infrastructure;

internal sealed class ClaimFactsReader(ClaimsDbContext dbContext) : IClaimFactsReader
{
    public async Task<ClaimFactEvidence> GetFieldEvidenceAsync(
        Guid organizationId,
        Guid claimId,
        string fieldName,
        CancellationToken cancellationToken)
    {
        var claim = await dbContext.Claims
            .AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == organizationId &&
                candidate.Id == claimId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.PolicyNumber,
                candidate.DateOfLoss,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (claim is null)
        {
            return new ClaimFactEvidence(
                ClaimFactEvidenceOutcome.Unavailable,
                Detail: "The tenant-scoped claim was not found.");
        }

        var isPresent = fieldName.Trim().ToUpperInvariant() switch
        {
            "POLICYNUMBER" => !string.IsNullOrWhiteSpace(claim.PolicyNumber),
            "DATEOFLOSS" => claim.DateOfLoss.HasValue,
            _ => (bool?)null,
        };

        return isPresent switch
        {
            true => new ClaimFactEvidence(
                ClaimFactEvidenceOutcome.Present,
                $"{claim.Id}:{fieldName}",
                $"Claim field '{fieldName}' is present."),
            false => new ClaimFactEvidence(
                ClaimFactEvidenceOutcome.Missing,
                Detail: $"Claim field '{fieldName}' is missing."),
            null => new ClaimFactEvidence(
                ClaimFactEvidenceOutcome.Unavailable,
                Detail: $"Claim field '{fieldName}' is not supported by the evidence contract."),
        };
    }
}
