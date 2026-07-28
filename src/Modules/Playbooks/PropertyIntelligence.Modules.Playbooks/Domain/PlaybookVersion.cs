using System.Text.Json;
using PropertyIntelligence.Modules.Playbooks.Contracts;

namespace PropertyIntelligence.Modules.Playbooks.Domain;

public sealed class PlaybookVersion
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private PlaybookVersion()
    {
    }

    internal PlaybookVersion(
        Guid organizationId,
        int version,
        string workflowType,
        int schemaVersion,
        IReadOnlyList<PlaybookStageDefinition> stages,
        IReadOnlyList<PlaybookAssignmentRuleDefinition> assignmentRules,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        PlaybookDefinitionValidator.Validate(workflowType, schemaVersion, stages, assignmentRules);
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Version = version;
        WorkflowType = workflowType;
        SchemaVersion = schemaVersion;
        DefinitionJson = JsonSerializer.Serialize(stages, JsonOptions);
        AssignmentRulesJson = JsonSerializer.Serialize(assignmentRules, JsonOptions);
        Status = PlaybookVersionStatus.Draft;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public int Version { get; private set; }
    public string WorkflowType { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; }
    public PlaybookVersionStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    internal string DefinitionJson { get; private set; } = "[]";
    internal string AssignmentRulesJson { get; private set; } = "[]";

    public IReadOnlyList<PlaybookStageDefinition> Stages =>
        JsonSerializer.Deserialize<PlaybookStageDefinition[]>(DefinitionJson, JsonOptions) ?? [];
    public IReadOnlyList<PlaybookAssignmentRuleDefinition> AssignmentRules =>
        JsonSerializer.Deserialize<PlaybookAssignmentRuleDefinition[]>(
            AssignmentRulesJson,
            JsonOptions) ?? [];

    internal void UpdateDraft(
        string workflowType,
        int schemaVersion,
        IReadOnlyList<PlaybookStageDefinition> stages,
        IReadOnlyList<PlaybookAssignmentRuleDefinition> assignmentRules)
    {
        EnsureDraft();
        PlaybookDefinitionValidator.Validate(workflowType, schemaVersion, stages, assignmentRules);
        WorkflowType = workflowType;
        SchemaVersion = schemaVersion;
        DefinitionJson = JsonSerializer.Serialize(stages, JsonOptions);
        AssignmentRulesJson = JsonSerializer.Serialize(assignmentRules, JsonOptions);
    }

    internal void Publish(Guid actorId, DateTimeOffset publishedAt)
    {
        EnsureDraft();
        Status = PlaybookVersionStatus.Published;
        PublishedBy = actorId;
        PublishedAt = publishedAt;
    }

    internal void Deprecate()
    {
        if (Status == PlaybookVersionStatus.Published)
        {
            Status = PlaybookVersionStatus.Deprecated;
        }
    }

    private void EnsureDraft()
    {
        if (Status != PlaybookVersionStatus.Draft)
        {
            throw new InvalidOperationException("Published playbook versions are immutable.");
        }
    }
}
