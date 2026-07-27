namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowOutboxMessage
{
    private WorkflowOutboxMessage() { }

    internal WorkflowOutboxMessage(
        Guid organizationId,
        Guid aggregateId,
        string eventType,
        string payload,
        DateTimeOffset occurredAt,
        string correlationId,
        string causationId)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        AggregateType = nameof(WorkflowInstance);
        AggregateId = aggregateId;
        EventType = eventType;
        Payload = payload;
        OccurredAt = occurredAt;
        AvailableAt = occurredAt;
        CorrelationId = correlationId;
        CausationId = causationId;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string CausationId { get; private set; } = string.Empty;

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void RecordFailure(string error, DateTimeOffset retryAt)
    {
        AttemptCount++;
        LastError = error;
        AvailableAt = retryAt;
    }
}
