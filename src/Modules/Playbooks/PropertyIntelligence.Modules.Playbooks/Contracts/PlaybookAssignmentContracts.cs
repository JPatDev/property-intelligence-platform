namespace PropertyIntelligence.Modules.Playbooks.Contracts;

public static class PlaybookAssignmentFacts
{
    public const string ClaimNumber = "claim.claimNumber";
    public const string PolicyNumber = "claim.policyNumber";
    public const string DateOfLoss = "claim.dateOfLoss";
    public const string Status = "claim.status";
    public const string PropertyId = "claim.propertyId";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(
        [ClaimNumber, PolicyNumber, DateOfLoss, Status, PropertyId],
        StringComparer.OrdinalIgnoreCase);
}

public static class PlaybookAssignmentOperators
{
    public const string EqualTo = "equals";
    public const string NotEquals = "notEquals";
    public const string In = "in";
    public const string NotIn = "notIn";
    public const string Exists = "exists";
    public const string NotExists = "notExists";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(
        [EqualTo, NotEquals, In, NotIn, Exists, NotExists],
        StringComparer.OrdinalIgnoreCase);
}

public sealed record PlaybookAssignmentRuleDefinition(
    Guid Id,
    string Name,
    int Priority,
    PlaybookConditionGroup When);

public sealed record PlaybookConditionGroup(
    string Match,
    IReadOnlyList<PlaybookFactCondition> Conditions,
    IReadOnlyList<PlaybookConditionGroup>? Groups = null);

public sealed record PlaybookFactCondition(
    string Fact,
    string Operator,
    IReadOnlyList<string>? Values = null);

public enum PlaybookAssignmentOutcome
{
    Matched = 1,
    NotMatched = 2,
    Indeterminate = 3,
}

public sealed record PlaybookConditionEvaluation(
    string Fact,
    string Operator,
    PlaybookAssignmentOutcome Outcome,
    string Explanation);

public sealed record PlaybookAssignmentRecommendation(
    Guid PlaybookId,
    Guid VersionId,
    string Key,
    int Version,
    string Name,
    string WorkflowType,
    int Priority,
    PlaybookAssignmentOutcome Outcome,
    string Explanation,
    IReadOnlyList<PlaybookConditionEvaluation> Conditions);
