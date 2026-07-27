namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowAuditRecord
{
    private WorkflowAuditRecord() { }

    internal WorkflowAuditRecord(
        Guid organizationId,
        Guid workflowId,
        string actorType,
        Guid? actorId,
        string action,
        string? previousState,
        string? newState,
        DateTimeOffset occurredAt,
        string correlationId,
        string causationId,
        bool systemGenerated)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        WorkflowId = workflowId;
        EntityType = nameof(WorkflowInstance);
        EntityId = workflowId;
        ActorType = actorType;
        ActorId = actorId;
        Action = action;
        PreviousState = previousState;
        NewState = newState;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        CausationId = causationId;
        SystemGenerated = systemGenerated;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string ActorType { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? PreviousState { get; private set; }
    public string? NewState { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string CausationId { get; private set; } = string.Empty;
    public bool SystemGenerated { get; private set; }
}
