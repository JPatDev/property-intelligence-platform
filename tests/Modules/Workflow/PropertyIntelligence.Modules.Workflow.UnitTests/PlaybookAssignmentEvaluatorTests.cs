using PropertyIntelligence.Modules.Playbooks.Application.Assignment;
using PropertyIntelligence.Modules.Playbooks.Contracts;

namespace PropertyIntelligence.Modules.Workflow.UnitTests;

public sealed class PlaybookAssignmentEvaluatorTests
{
    [Fact]
    public void All_group_matches_when_every_condition_matches()
    {
        var rule = Group(
            "all",
            new PlaybookFactCondition(
                PlaybookAssignmentFacts.Status,
                PlaybookAssignmentOperators.EqualTo,
                ["Intake"]),
            new PlaybookFactCondition(
                PlaybookAssignmentFacts.PolicyNumber,
                PlaybookAssignmentOperators.Exists));
        var facts = Facts(("claim.status", "Intake"), ("claim.policyNumber", "P-100"));

        var result = PlaybookAssignmentEvaluator.Evaluate(rule, facts);

        Assert.Equal(PlaybookAssignmentOutcome.Matched, result.Outcome);
        Assert.All(
            result.Conditions,
            condition => Assert.Equal(PlaybookAssignmentOutcome.Matched, condition.Outcome));
    }

    [Fact]
    public void Any_group_matches_when_one_condition_matches()
    {
        var rule = Group(
            "any",
            new PlaybookFactCondition(
                PlaybookAssignmentFacts.Status,
                PlaybookAssignmentOperators.EqualTo,
                ["Closed"]),
            new PlaybookFactCondition(
                PlaybookAssignmentFacts.PolicyNumber,
                PlaybookAssignmentOperators.Exists));
        var facts = Facts(("claim.status", "Intake"), ("claim.policyNumber", "P-100"));

        var result = PlaybookAssignmentEvaluator.Evaluate(rule, facts);

        Assert.Equal(PlaybookAssignmentOutcome.Matched, result.Outcome);
    }

    [Fact]
    public void Missing_authoritative_fact_is_known_and_can_match_not_exists()
    {
        var rule = Group(
            "all",
            new PlaybookFactCondition(
                PlaybookAssignmentFacts.PolicyNumber,
                PlaybookAssignmentOperators.NotExists));
        var facts = Facts(("claim.policyNumber", null));

        var result = PlaybookAssignmentEvaluator.Evaluate(rule, facts);

        Assert.Equal(PlaybookAssignmentOutcome.Matched, result.Outcome);
    }

    [Fact]
    public void Unavailable_fact_makes_a_required_all_group_indeterminate()
    {
        var rule = Group(
            "all",
            new PlaybookFactCondition(
                PlaybookAssignmentFacts.Status,
                PlaybookAssignmentOperators.EqualTo,
                ["Intake"]));
        var facts = Facts();

        var result = PlaybookAssignmentEvaluator.Evaluate(rule, facts);

        Assert.Equal(PlaybookAssignmentOutcome.Indeterminate, result.Outcome);
    }

    private static PlaybookConditionGroup Group(
        string match,
        params PlaybookFactCondition[] conditions) =>
        new(match, conditions);

    private static AssignmentFactSet Facts(params (string Name, string? Value)[] facts) =>
        new(new Dictionary<string, string?>(
            facts.Select(fact => new KeyValuePair<string, string?>(fact.Name, fact.Value)),
            StringComparer.OrdinalIgnoreCase));
}
