namespace PropertyIntelligence.Modules.Playbooks.Contracts;

public sealed record PublishedPlaybookDefinition(
    Guid PlaybookId,
    Guid VersionId,
    Guid OrganizationId,
    string Key,
    int Version,
    string Name,
    string Description,
    string WorkflowType,
    int SchemaVersion,
    IReadOnlyList<PlaybookStageDefinition> Stages,
    IReadOnlyList<PlaybookAssignmentRuleDefinition> AssignmentRules);

public sealed record PublishedPlaybookSummary(
    Guid PlaybookId,
    Guid VersionId,
    string Key,
    int Version,
    string Name,
    string Description,
    string WorkflowType,
    DateTimeOffset PublishedAt);

public sealed record PlaybookStageDefinition(
    Guid Id,
    string Name,
    int Order,
    bool IsOptional,
    IReadOnlyList<PlaybookTaskDefinition> Tasks);

public sealed record PlaybookTaskDefinition(
    Guid Id,
    string Name,
    int Order,
    int Priority,
    bool IsRequired,
    IReadOnlyCollection<Guid> DependencyIds,
    IReadOnlyList<PlaybookGateDefinition> CompletionGates);

public sealed record PlaybookGateDefinition(
    Guid Id,
    string GateType,
    string Scope,
    string Severity,
    IReadOnlyDictionary<string, string> Parameters,
    string FailureCode,
    string FailureMessage,
    int EvaluationVersion);

public interface IPublishedPlaybookReader
{
    Task<PublishedPlaybookDefinition?> FindAsync(
        Guid organizationId,
        string key,
        int version,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishedPlaybookSummary>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
}
