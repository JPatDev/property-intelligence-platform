using PropertyIntelligence.Modules.Playbooks.Contracts;

namespace PropertyIntelligence.Modules.Playbooks.Application.Assignment;

internal sealed record AssignmentFactSet(
    IReadOnlyDictionary<string, string?> Values);

internal sealed record AssignmentRuleEvaluation(
    PlaybookAssignmentOutcome Outcome,
    IReadOnlyList<PlaybookConditionEvaluation> Conditions);

internal static class PlaybookAssignmentEvaluator
{
    public static AssignmentRuleEvaluation Evaluate(
        PlaybookConditionGroup group,
        AssignmentFactSet facts)
    {
        var evaluations = new List<PlaybookConditionEvaluation>();
        var outcomes = new List<PlaybookAssignmentOutcome>();

        foreach (var condition in group.Conditions)
        {
            var evaluation = EvaluateCondition(condition, facts);
            evaluations.Add(evaluation);
            outcomes.Add(evaluation.Outcome);
        }

        foreach (var child in group.Groups ?? [])
        {
            var evaluation = Evaluate(child, facts);
            evaluations.AddRange(evaluation.Conditions);
            outcomes.Add(evaluation.Outcome);
        }

        var outcome = group.Match.Equals("any", StringComparison.OrdinalIgnoreCase)
            ? EvaluateAny(outcomes)
            : EvaluateAll(outcomes);
        return new AssignmentRuleEvaluation(outcome, evaluations);
    }

    private static PlaybookConditionEvaluation EvaluateCondition(
        PlaybookFactCondition condition,
        AssignmentFactSet facts)
    {
        if (!PlaybookAssignmentFacts.Supported.Contains(condition.Fact) ||
            !facts.Values.TryGetValue(condition.Fact, out var actual))
        {
            return Result(
                condition,
                PlaybookAssignmentOutcome.Indeterminate,
                $"Fact '{condition.Fact}' is not available from the authoritative source.");
        }

        var values = condition.Values ?? [];
        var outcome = condition.Operator.ToLowerInvariant() switch
        {
            "exists" => actual is not null
                ? PlaybookAssignmentOutcome.Matched
                : PlaybookAssignmentOutcome.NotMatched,
            "notexists" => actual is null
                ? PlaybookAssignmentOutcome.Matched
                : PlaybookAssignmentOutcome.NotMatched,
            "equals" => Compare(actual, values.FirstOrDefault())
                ? PlaybookAssignmentOutcome.Matched
                : PlaybookAssignmentOutcome.NotMatched,
            "notequals" => !Compare(actual, values.FirstOrDefault())
                ? PlaybookAssignmentOutcome.Matched
                : PlaybookAssignmentOutcome.NotMatched,
            "in" => values.Any(value => Compare(actual, value))
                ? PlaybookAssignmentOutcome.Matched
                : PlaybookAssignmentOutcome.NotMatched,
            "notin" => values.All(value => !Compare(actual, value))
                ? PlaybookAssignmentOutcome.Matched
                : PlaybookAssignmentOutcome.NotMatched,
            _ => PlaybookAssignmentOutcome.Indeterminate,
        };

        return Result(
            condition,
            outcome,
            outcome switch
            {
                PlaybookAssignmentOutcome.Matched =>
                    $"Fact '{condition.Fact}' satisfied '{condition.Operator}'.",
                PlaybookAssignmentOutcome.NotMatched =>
                    $"Fact '{condition.Fact}' did not satisfy '{condition.Operator}'.",
                _ => $"Operator '{condition.Operator}' is not supported.",
            });
    }

    private static bool Compare(string? left, string? right) =>
        left is not null &&
        right is not null &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static PlaybookConditionEvaluation Result(
        PlaybookFactCondition condition,
        PlaybookAssignmentOutcome outcome,
        string explanation) =>
        new(condition.Fact, condition.Operator, outcome, explanation);

    private static PlaybookAssignmentOutcome EvaluateAll(
        IReadOnlyCollection<PlaybookAssignmentOutcome> outcomes)
    {
        if (outcomes.Contains(PlaybookAssignmentOutcome.NotMatched))
        {
            return PlaybookAssignmentOutcome.NotMatched;
        }

        return outcomes.Contains(PlaybookAssignmentOutcome.Indeterminate)
            ? PlaybookAssignmentOutcome.Indeterminate
            : PlaybookAssignmentOutcome.Matched;
    }

    private static PlaybookAssignmentOutcome EvaluateAny(
        IReadOnlyCollection<PlaybookAssignmentOutcome> outcomes)
    {
        if (outcomes.Contains(PlaybookAssignmentOutcome.Matched))
        {
            return PlaybookAssignmentOutcome.Matched;
        }

        return outcomes.Contains(PlaybookAssignmentOutcome.Indeterminate)
            ? PlaybookAssignmentOutcome.Indeterminate
            : PlaybookAssignmentOutcome.NotMatched;
    }
}
