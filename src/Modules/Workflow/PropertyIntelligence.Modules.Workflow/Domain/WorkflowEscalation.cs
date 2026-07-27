namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowEscalation
{
    private WorkflowEscalation()
    {
    }

    internal WorkflowEscalation(
        Guid organizationId,
        Guid workflowId,
        Guid? taskId,
        WorkflowEscalationType type,
        WorkflowEscalationSeverity severity,
        string deduplicationKey,
        string message,
        DateTimeOffset triggeredAt)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        WorkflowId = workflowId;
        TaskId = taskId;
        Type = type;
        Severity = severity;
        DeduplicationKey = deduplicationKey;
        Message = message;
        Status = WorkflowEscalationStatus.Open;
        TriggeredAt = triggeredAt;
        LastObservedAt = triggeredAt;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public Guid? TaskId { get; private set; }
    public WorkflowEscalationType Type { get; private set; }
    public WorkflowEscalationSeverity Severity { get; private set; }
    public WorkflowEscalationStatus Status { get; private set; }
    public string DeduplicationKey { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset TriggeredAt { get; private set; }
    public DateTimeOffset LastObservedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionReason { get; private set; }

    internal void Observe(WorkflowEscalationSeverity severity, string message, DateTimeOffset observedAt)
    {
        Severity = severity;
        Message = message;
        LastObservedAt = observedAt;
        if (Status == WorkflowEscalationStatus.Resolved)
        {
            Status = WorkflowEscalationStatus.Open;
            TriggeredAt = observedAt;
            ResolvedAt = null;
            ResolutionReason = null;
        }
    }

    internal void Acknowledge(Guid actorId, DateTimeOffset acknowledgedAt)
    {
        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (Status == WorkflowEscalationStatus.Resolved)
        {
            throw new WorkflowDomainException(
                "workflow.escalation_resolved",
                "A resolved escalation cannot be acknowledged.");
        }

        Status = WorkflowEscalationStatus.Acknowledged;
        AcknowledgedBy = actorId;
        AcknowledgedAt = acknowledgedAt;
    }

    internal void Resolve(DateTimeOffset resolvedAt, string reason)
    {
        if (Status == WorkflowEscalationStatus.Resolved)
        {
            return;
        }

        Status = WorkflowEscalationStatus.Resolved;
        ResolvedAt = resolvedAt;
        ResolutionReason = reason;
    }
}
