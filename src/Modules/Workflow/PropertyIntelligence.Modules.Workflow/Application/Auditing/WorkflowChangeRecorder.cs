using System.Text.Json;
using PropertyIntelligence.Modules.Workflow.Application.Outbox;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Auditing;

internal interface IWorkflowChangeRecorder
{
    void Record(
        Guid organizationId,
        Guid workflowId,
        string action,
        Guid? actorId,
        bool systemGenerated,
        string? previousState,
        string? newState,
        IReadOnlyList<string> changedEntityTypes,
        long? workflowVersion,
        DateTimeOffset occurredAt,
        string correlationId,
        string causationId);
}

internal sealed class WorkflowChangeRecorder(WorkflowDbContext dbContext) : IWorkflowChangeRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Record(
        Guid organizationId,
        Guid workflowId,
        string action,
        Guid? actorId,
        bool systemGenerated,
        string? previousState,
        string? newState,
        IReadOnlyList<string> changedEntityTypes,
        long? workflowVersion,
        DateTimeOffset occurredAt,
        string correlationId,
        string causationId)
    {
        var actorType = systemGenerated ? "System" :
            actorId.HasValue ? "User" : "Unattributed";
        dbContext.AuditRecords.Add(new WorkflowAuditRecord(
            organizationId,
            workflowId,
            actorType,
            actorId,
            action,
            previousState,
            newState,
            occurredAt,
            correlationId,
            causationId,
            systemGenerated));

        var integrationEvent = new WorkflowChangedIntegrationEvent(
            1,
            organizationId,
            workflowId,
            action,
            workflowVersion,
            changedEntityTypes,
            occurredAt,
            correlationId,
            causationId);
        dbContext.OutboxMessages.Add(new WorkflowOutboxMessage(
            organizationId,
            workflowId,
            typeof(WorkflowChangedIntegrationEvent).FullName!,
            JsonSerializer.Serialize(integrationEvent, JsonOptions),
            occurredAt,
            correlationId,
            causationId));
    }
}
