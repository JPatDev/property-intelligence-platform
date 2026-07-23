namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowBlocker
{
    internal WorkflowBlocker(Guid id, string code, string description, Guid createdBy, DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Description { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }
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
