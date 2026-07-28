using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Playbooks.Contracts;
using PropertyIntelligence.Modules.Playbooks.Domain;
using PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Playbooks.Infrastructure;

internal sealed class PublishedPlaybookReader(PlaybooksDbContext dbContext)
    : IPublishedPlaybookReader
{
    public async Task<PublishedPlaybookDefinition?> FindAsync(
        Guid organizationId,
        string key,
        int version,
        CancellationToken cancellationToken)
    {
        var playbook = await dbContext.Playbooks
            .AsNoTracking()
            .Include(candidate => candidate.Versions)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == organizationId &&
                    candidate.Key == key.Trim().ToLower() &&
                    candidate.Status == PlaybookStatus.Active,
                cancellationToken);
        var published = playbook?.Versions.SingleOrDefault(candidate =>
            candidate.Version == version &&
            candidate.Status == PlaybookVersionStatus.Published);
        return playbook is null || published is null
            ? null
            : ToDefinition(playbook, published);
    }

    public async Task<IReadOnlyList<PublishedPlaybookSummary>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var playbooks = await dbContext.Playbooks
            .AsNoTracking()
            .Include(playbook => playbook.Versions)
            .Where(playbook =>
                playbook.OrganizationId == organizationId &&
                playbook.Status == PlaybookStatus.Active)
            .OrderBy(playbook => playbook.Key)
            .ToListAsync(cancellationToken);

        return playbooks
            .SelectMany(playbook => playbook.Versions
                .Where(version => version.Status == PlaybookVersionStatus.Published)
                .Select(version => new PublishedPlaybookSummary(
                    playbook.Id,
                    version.Id,
                    playbook.Key,
                    version.Version,
                    playbook.Name,
                    playbook.Description,
                    version.WorkflowType,
                    version.PublishedAt!.Value)))
            .OrderBy(summary => summary.Key)
            .ThenByDescending(summary => summary.Version)
            .ToArray();
    }

    private static PublishedPlaybookDefinition ToDefinition(
        Playbook playbook,
        PlaybookVersion version) =>
        new(
            playbook.Id,
            version.Id,
            playbook.OrganizationId,
            playbook.Key,
            version.Version,
            playbook.Name,
            playbook.Description,
            version.WorkflowType,
            version.SchemaVersion,
            version.Stages,
            version.AssignmentRules);
}
