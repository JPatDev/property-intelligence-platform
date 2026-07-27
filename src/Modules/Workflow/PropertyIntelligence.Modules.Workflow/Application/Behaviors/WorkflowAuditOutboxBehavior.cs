using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Workflow.Application.Abstractions;
using PropertyIntelligence.Modules.Workflow.Application.Auditing;
using PropertyIntelligence.Modules.Workflow.Application.Commands;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Behaviors;

internal sealed class WorkflowAuditOutboxBehavior<TRequest, TResponse>(
    WorkflowDbContext dbContext,
    IWorkflowChangeRecorder recorder,
    TimeProvider timeProvider,
    IServiceProvider serviceProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IWorkflowCommand<TResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> ExcludedProperties =
    [
        nameof(CompletionGateDefinition.Parameters),
        nameof(WorkflowOutboxMessage.Payload),
        nameof(WorkflowAuditRecord.PreviousState),
        nameof(WorkflowAuditRecord.NewState),
        nameof(WorkflowOutboxMessage.LastError),
    ];

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();
        dbContext.ChangeTracker.DetectChanges();

        var changedEntries = dbContext.ChangeTracker.Entries()
            .Where(entry => entry.Entity is not WorkflowAuditRecord and not WorkflowOutboxMessage)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        if (changedEntries.Length == 0)
        {
            return response;
        }

        var workflowId = ResolveWorkflowId(request, response, changedEntries);
        var organizationId = ResolveOrganizationId(request, changedEntries);
        if (workflowId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new InvalidOperationException("A workflow audit record requires workflow and organization IDs.");
        }

        var changeSets = changedEntries.Select(ToChangeSet).ToArray();
        var actorId = ResolveGuidProperty(request, "ActorId") ?? ResolveAuthenticatedActor();
        var action = typeof(TRequest).Name.Replace("Command", string.Empty, StringComparison.Ordinal);
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var causationId = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        var workflowVersion = changedEntries
            .Select(entry => entry.Entity)
            .OfType<WorkflowInstance>()
            .Select(workflow => (long?)workflow.Version)
            .FirstOrDefault();

        recorder.Record(
            organizationId,
            workflowId,
            action,
            actorId,
            false,
            JsonSerializer.Serialize(
                changeSets.Select(change => new
                {
                    change.EntityType,
                    change.EntityId,
                    change.State,
                    Values = change.PreviousValues,
                }),
                JsonOptions),
            JsonSerializer.Serialize(
                changeSets.Select(change => new
                {
                    change.EntityType,
                    change.EntityId,
                    change.State,
                    Values = change.CurrentValues,
                }),
                JsonOptions),
            changeSets.Select(change => change.EntityType).Distinct().Order().ToArray(),
            workflowVersion,
            timeProvider.GetUtcNow(),
            correlationId,
            causationId);

        return response;
    }

    private Guid? ResolveAuthenticatedActor()
    {
        try
        {
            return serviceProvider.GetService<IRequestIdentity>()?.UserId;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static ChangedEntity ToChangeSet(EntityEntry entry)
    {
        var properties = entry.Properties
            .Where(property => !ExcludedProperties.Contains(property.Metadata.Name))
            .Where(property => entry.State != EntityState.Modified || property.IsModified)
            .ToArray();
        return new ChangedEntity(
            entry.Metadata.ClrType.Name,
            ResolveEntityId(entry),
            entry.State.ToString(),
            properties.ToDictionary(
                property => property.Metadata.Name,
                property => entry.State == EntityState.Added ? null : property.OriginalValue),
            properties.ToDictionary(
                property => property.Metadata.Name,
                property => entry.State == EntityState.Deleted ? null : property.CurrentValue));
    }

    private static Guid ResolveWorkflowId(
        TRequest request,
        TResponse response,
        IReadOnlyCollection<EntityEntry> entries)
    {
        if (request is IExistingWorkflowCommand<TResponse> existing)
        {
            return existing.WorkflowId;
        }

        if (request is CreateWorkflowCommand && response is Guid createdWorkflowId)
        {
            return createdWorkflowId;
        }

        return entries
            .Select(entry => entry.Entity)
            .OfType<WorkflowEscalation>()
            .Select(escalation => escalation.WorkflowId)
            .FirstOrDefault();
    }

    private static Guid ResolveOrganizationId(
        TRequest request,
        IReadOnlyCollection<EntityEntry> entries) =>
        ResolveGuidProperty(request, "OrganizationId") ??
        entries.Select(entry => ResolveGuidProperty(entry.Entity, "OrganizationId"))
            .FirstOrDefault(value => value.HasValue) ??
        Guid.Empty;

    private static Guid? ResolveGuidProperty(object instance, string propertyName)
    {
        var value = instance.GetType().GetProperty(propertyName)?.GetValue(instance);
        return value is Guid guid && guid != Guid.Empty ? guid : null;
    }

    private static string ResolveEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        return key is null
            ? string.Empty
            : string.Join(
                ":",
                key.Properties.Select(property =>
                    entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty));
    }

    private sealed record ChangedEntity(
        string EntityType,
        string EntityId,
        string State,
        IReadOnlyDictionary<string, object?> PreviousValues,
        IReadOnlyDictionary<string, object?> CurrentValues);
}
