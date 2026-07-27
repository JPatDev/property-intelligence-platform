namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class NextActionSnapshot
{
    private NextActionSnapshot()
    {
    }

    internal NextActionSnapshot(
        Guid workflowId,
        Guid organizationId,
        Guid taskId,
        string action,
        Guid? ownerId,
        DateTimeOffset? dueAt,
        int calculatedPriority,
        string reasonCode,
        string reason,
        DateTimeOffset calculatedAt,
        int calculationVersion)
    {
        WorkflowId = workflowId;
        OrganizationId = organizationId;
        Update(
            taskId,
            action,
            ownerId,
            dueAt,
            calculatedPriority,
            reasonCode,
            reason,
            calculatedAt,
            calculationVersion);
    }

    public Guid WorkflowId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid TaskId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid? OwnerId { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public int CalculatedPriority { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; private set; }
    public int CalculationVersion { get; private set; }

    internal void Update(
        Guid taskId,
        string action,
        Guid? ownerId,
        DateTimeOffset? dueAt,
        int calculatedPriority,
        string reasonCode,
        string reason,
        DateTimeOffset calculatedAt,
        int calculationVersion)
    {
        TaskId = taskId;
        Action = action;
        OwnerId = ownerId;
        DueAt = dueAt;
        CalculatedPriority = calculatedPriority;
        ReasonCode = reasonCode;
        Reason = reason;
        CalculatedAt = calculatedAt;
        CalculationVersion = calculationVersion;
    }
}
