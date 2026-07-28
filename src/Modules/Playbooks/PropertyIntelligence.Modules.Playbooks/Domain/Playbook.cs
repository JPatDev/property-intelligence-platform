using PropertyIntelligence.Modules.Playbooks.Contracts;

namespace PropertyIntelligence.Modules.Playbooks.Domain;

public sealed class Playbook
{
    private readonly List<PlaybookVersion> _versions = [];

    private Playbook()
    {
    }

    private Playbook(
        Guid organizationId,
        string key,
        string name,
        string description,
        string workflowType,
        int schemaVersion,
        IReadOnlyList<PlaybookStageDefinition> stages,
        IReadOnlyList<PlaybookAssignmentRuleDefinition> assignmentRules,
        Guid actorId,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        OrganizationId = RequireId(organizationId, "organization");
        Key = NormalizeKey(key);
        Name = Normalize(name, "name", 200);
        Description = Normalize(description, "description", 2000);
        Status = PlaybookStatus.Draft;
        CreatedBy = RequireId(actorId, "actor");
        CreatedAt = createdAt;
        Version = 1;
        _versions.Add(new PlaybookVersion(
            organizationId,
            1,
            workflowType,
            schemaVersion,
            stages,
            assignmentRules,
            actorId,
            createdAt));
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PlaybookStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyList<PlaybookVersion> Versions => _versions.AsReadOnly();

    public static Playbook Create(
        Guid organizationId,
        string key,
        string name,
        string description,
        string workflowType,
        int schemaVersion,
        IReadOnlyList<PlaybookStageDefinition> stages,
        Guid actorId,
        DateTimeOffset createdAt,
        IReadOnlyList<PlaybookAssignmentRuleDefinition>? assignmentRules = null) =>
        new(
            organizationId,
            key,
            name,
            description,
            workflowType,
            schemaVersion,
            stages,
            assignmentRules ?? [],
            actorId,
            createdAt);

    public void UpdateDraft(
        Guid versionId,
        string name,
        string description,
        string workflowType,
        int schemaVersion,
        IReadOnlyList<PlaybookStageDefinition> stages,
        Guid actorId,
        DateTimeOffset modifiedAt,
        IReadOnlyList<PlaybookAssignmentRuleDefinition>? assignmentRules = null)
    {
        var version = FindVersion(versionId);
        version.UpdateDraft(workflowType, schemaVersion, stages, assignmentRules ?? []);
        Name = Normalize(name, "name", 200);
        Description = Normalize(description, "description", 2000);
        ModifiedBy = RequireId(actorId, "actor");
        ModifiedAt = modifiedAt;
        Version++;
    }

    public void Publish(Guid versionId, Guid actorId, DateTimeOffset publishedAt)
    {
        var version = FindVersion(versionId);
        foreach (var published in _versions.Where(candidate =>
                     candidate.Status == PlaybookVersionStatus.Published))
        {
            published.Deprecate();
        }

        version.Publish(RequireId(actorId, "actor"), publishedAt);
        Status = PlaybookStatus.Active;
        ModifiedBy = actorId;
        ModifiedAt = publishedAt;
        Version++;
    }

    public Guid CreateDraft(Guid actorId, DateTimeOffset createdAt)
    {
        if (_versions.Any(version => version.Status == PlaybookVersionStatus.Draft))
        {
            throw new InvalidOperationException("The playbook already has a draft version.");
        }

        var source = _versions
            .OrderByDescending(version => version.Version)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("The playbook has no source version.");
        var draft = new PlaybookVersion(
            OrganizationId,
            source.Version + 1,
            source.WorkflowType,
            source.SchemaVersion,
            source.Stages,
            source.AssignmentRules,
            RequireId(actorId, "actor"),
            createdAt);
        _versions.Add(draft);
        ModifiedBy = actorId;
        ModifiedAt = createdAt;
        Version++;
        return draft.Id;
    }

    private PlaybookVersion FindVersion(Guid versionId) =>
        _versions.SingleOrDefault(version => version.Id == versionId)
        ?? throw new InvalidOperationException("The playbook version was not found.");

    private static Guid RequireId(Guid value, string name) =>
        value != Guid.Empty ? value : throw new ArgumentException($"A {name} ID is required.", name);

    private static string NormalizeKey(string value)
    {
        var normalized = Normalize(value, "key", 100).ToLowerInvariant();
        if (normalized.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException(
                "A playbook key may contain only letters, numbers, hyphens, and underscores.",
                nameof(value));
        }

        return normalized;
    }

    private static string Normalize(string value, string name, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized) || normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"A {name} between 1 and {maximumLength} characters is required.",
                name);
        }

        return normalized;
    }
}
