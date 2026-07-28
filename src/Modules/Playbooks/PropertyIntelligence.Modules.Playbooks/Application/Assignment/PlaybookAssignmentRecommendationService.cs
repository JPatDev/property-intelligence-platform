using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Claims.Contracts;
using PropertyIntelligence.Modules.Playbooks.Contracts;
using PropertyIntelligence.Modules.Playbooks.Domain;
using PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Playbooks.Application.Assignment;

internal sealed class PlaybookAssignmentRecommendationService(
    PlaybooksDbContext dbContext,
    IClaimFactsReader claimFactsReader,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PlaybookAssignmentRecommendation>?> RecommendAsync(
        Guid organizationId,
        Guid claimId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var claim = await claimFactsReader.GetAssignmentFactsAsync(
            organizationId,
            claimId,
            cancellationToken);
        if (claim is null)
        {
            return null;
        }

        var factSet = new AssignmentFactSet(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [PlaybookAssignmentFacts.ClaimNumber] = claim.ClaimNumber,
                [PlaybookAssignmentFacts.PolicyNumber] = claim.PolicyNumber,
                [PlaybookAssignmentFacts.DateOfLoss] =
                    claim.DateOfLoss?.ToString("yyyy-MM-dd"),
                [PlaybookAssignmentFacts.Status] = claim.Status,
                [PlaybookAssignmentFacts.PropertyId] = claim.PropertyId.ToString(),
            });
        var playbooks = await dbContext.Playbooks
            .AsNoTracking()
            .Include(playbook => playbook.Versions)
            .Where(playbook =>
                playbook.OrganizationId == organizationId &&
                playbook.Status == PlaybookStatus.Active)
            .ToListAsync(cancellationToken);

        var recommendations = playbooks
            .Select(playbook =>
            {
                var version = playbook.Versions.Single(candidate =>
                    candidate.Status == PlaybookVersionStatus.Published);
                return Evaluate(playbook, version, factSet);
            })
            .OrderBy(result => result.Outcome == PlaybookAssignmentOutcome.Matched ? 0 : 1)
            .ThenByDescending(result => result.Priority)
            .ThenBy(result => result.Name)
            .ToArray();

        dbContext.AssignmentEvaluations.Add(new PlaybookAssignmentEvaluationRecord(
            organizationId,
            claimId,
            actorId,
            timeProvider.GetUtcNow(),
            JsonSerializer.Serialize(recommendations, JsonOptions)));
        await dbContext.SaveChangesAsync(cancellationToken);
        return recommendations;
    }

    private static PlaybookAssignmentRecommendation Evaluate(
        Playbook playbook,
        PlaybookVersion version,
        AssignmentFactSet facts)
    {
        if (version.AssignmentRules.Count == 0)
        {
            return new PlaybookAssignmentRecommendation(
                playbook.Id,
                version.Id,
                playbook.Key,
                version.Version,
                playbook.Name,
                version.WorkflowType,
                0,
                PlaybookAssignmentOutcome.NotMatched,
                "The playbook has no automatic assignment rules and remains manually selectable.",
                []);
        }

        var rules = version.AssignmentRules
            .Select(rule => new
            {
                Rule = rule,
                Evaluation = PlaybookAssignmentEvaluator.Evaluate(rule.When, facts),
            })
            .OrderByDescending(result => result.Rule.Priority)
            .ToArray();
        var selected = rules.FirstOrDefault(result =>
                           result.Evaluation.Outcome == PlaybookAssignmentOutcome.Matched)
                       ?? rules.FirstOrDefault(result =>
                           result.Evaluation.Outcome ==
                           PlaybookAssignmentOutcome.Indeterminate)
                       ?? rules[0];
        var outcome = rules.Any(result =>
            result.Evaluation.Outcome == PlaybookAssignmentOutcome.Matched)
            ? PlaybookAssignmentOutcome.Matched
            : rules.Any(result =>
                result.Evaluation.Outcome == PlaybookAssignmentOutcome.Indeterminate)
                ? PlaybookAssignmentOutcome.Indeterminate
                : PlaybookAssignmentOutcome.NotMatched;

        return new PlaybookAssignmentRecommendation(
            playbook.Id,
            version.Id,
            playbook.Key,
            version.Version,
            playbook.Name,
            version.WorkflowType,
            selected.Rule.Priority,
            outcome,
            outcome switch
            {
                PlaybookAssignmentOutcome.Matched =>
                    $"Assignment rule '{selected.Rule.Name}' matched.",
                PlaybookAssignmentOutcome.Indeterminate =>
                    $"Assignment rule '{selected.Rule.Name}' could not be fully evaluated.",
                _ => "No assignment rule matched.",
            },
            selected.Evaluation.Conditions);
    }
}
