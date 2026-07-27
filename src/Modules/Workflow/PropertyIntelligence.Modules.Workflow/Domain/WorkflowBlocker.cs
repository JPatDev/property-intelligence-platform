namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowBlocker
{
    private WorkflowBlocker()
    {
    }

    internal WorkflowBlocker(
        Guid id,
        Guid organizationId,
        string code,
        string description,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionReason { get; private set; }
    public bool IsResolved => ResolvedAt.HasValue;

    internal void Resolve(Guid actorId, string reason, DateTimeOffset resolvedAt)
    {
        if (IsResolved)
        {
            throw new WorkflowDomainException("workflow.blocker_already_resolved", "The blocker is already resolved.");
        }

        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new WorkflowDomainException("workflow.reason_required", "A resolution reason is required.");
        }

        ResolvedBy = actorId;
        ResolvedAt = resolvedAt;
        ResolutionReason = reason.Trim();
    }
}
