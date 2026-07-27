namespace PropertyIntelligence.Modules.Workflow.Application.Outbox;

public sealed record WorkflowChangedIntegrationEvent(
    int SchemaVersion,
    Guid OrganizationId,
    Guid WorkflowId,
    string Action,
    long? WorkflowVersion,
    IReadOnlyList<string> ChangedEntityTypes,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string CausationId);

public sealed record OutboxMessageEnvelope(
    Guid Id,
    Guid OrganizationId,
    string AggregateType,
    Guid AggregateId,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string CausationId);

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken);
}
