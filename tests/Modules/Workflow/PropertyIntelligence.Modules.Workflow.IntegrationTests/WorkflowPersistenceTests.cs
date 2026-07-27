using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.Modules.Claims.Domain;
using PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;
using PropertyIntelligence.Modules.Communications.Domain;
using PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;
using PropertyIntelligence.Modules.Documents.Domain;
using PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;
using PropertyIntelligence.Modules.Workflow.Application.Commands;
using PropertyIntelligence.Modules.Workflow.Application.Queries;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.IntegrationTests;

public sealed class WorkflowPersistenceTests : IClassFixture<PostgreSqlWorkflowFixture>
{
    private readonly PostgreSqlWorkflowFixture _fixture;

    public WorkflowPersistenceTests(PostgreSqlWorkflowFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Authoritative_evidence_completes_property_claim_intake_workflow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var claimsDbContext = _fixture.Services.GetRequiredService<ClaimsDbContext>();
        var sender = _fixture.Services.GetRequiredService<ISender>();
        var claim = Claim.Create(
            organizationId,
            Guid.NewGuid(),
            $"CLM-{Guid.NewGuid():N}",
            "POL-12345",
            new DateOnly(2026, 7, 1),
            actorId,
            DateTimeOffset.UtcNow);
        claimsDbContext.Claims.Add(claim);
        await claimsDbContext.SaveChangesAsync(cancellationToken);

        var workflowId = await sender.Send(
            new CreateWorkflowCommand(
                organizationId,
                claim.Id,
                WorkflowType.Primary,
                "property-claim-intake",
                1),
            cancellationToken);
        var created = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);
        await sender.Send(
            new StartWorkflowCommand(organizationId, workflowId, created.Version),
            cancellationToken);
        var started = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);
        var firstTask = started.Stages
            .OrderBy(stage => stage.Order)
            .First()
            .Tasks
            .OrderBy(task => task.Order)
            .First();
        await sender.Send(
            new AssignTaskCommand(
                organizationId,
                workflowId,
                firstTask.Id,
                actorId,
                started.Version),
            cancellationToken);

        var result = await sender.Send(
            new CompleteTaskCommand(
                organizationId,
                workflowId,
                firstTask.Id,
                actorId,
                started.Version + 1),
            cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Failures);
        var updated = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);
        Assert.Equal(
            WorkflowTaskStatus.Completed,
            updated.Stages
                .SelectMany(stage => stage.Tasks)
                .Single(task => task.Id == firstTask.Id)
                .Status);

        var document = DocumentRecord.Register(
            organizationId,
            claim.Id,
            "SignedPaAgreement",
            "signed-pa-agreement.pdf",
            $"claims/{claim.Id}/signed-pa-agreement.pdf",
            actorId,
            DateTimeOffset.UtcNow);
        document.ChangeStatus(DocumentStatus.Verified, actorId, DateTimeOffset.UtcNow);
        var documentsDbContext = _fixture.Services.GetRequiredService<DocumentsDbContext>();
        documentsDbContext.Documents.Add(document);
        await documentsDbContext.SaveChangesAsync(cancellationToken);

        var agreementTask = updated.Stages
            .OrderBy(stage => stage.Order)
            .First()
            .Tasks
            .OrderBy(task => task.Order)
            .Skip(1)
            .First();
        await sender.Send(
            new AssignTaskCommand(
                organizationId,
                workflowId,
                agreementTask.Id,
                actorId,
                updated.Version),
            cancellationToken);
        var agreementResult = await sender.Send(
            new CompleteTaskCommand(
                organizationId,
                workflowId,
                agreementTask.Id,
                actorId,
                updated.Version + 1),
            cancellationToken);

        Assert.True(agreementResult.Succeeded);
        Assert.Empty(agreementResult.Failures);
        var intakeCompleted = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);
        Assert.Equal(WorkflowStageStatus.Completed, intakeCompleted.Stages[0].Status);
        Assert.Equal(WorkflowStageStatus.Active, intakeCompleted.Stages[1].Status);

        var communication = CommunicationRecord.Create(
            organizationId,
            claim.Id,
            "CarrierNotificationPacket",
            CommunicationDirection.Outbound,
            CommunicationChannel.Email,
            "Notice of representation",
            "claims@carrier.example",
            actorId,
            DateTimeOffset.UtcNow);
        communication.ChangeStatus(CommunicationStatus.Sent, actorId, DateTimeOffset.UtcNow);
        var communicationsDbContext =
            _fixture.Services.GetRequiredService<CommunicationsDbContext>();
        communicationsDbContext.Communications.Add(communication);
        await communicationsDbContext.SaveChangesAsync(cancellationToken);

        var notificationTask = intakeCompleted.Stages[1].Tasks.Single();
        await sender.Send(
            new AssignTaskCommand(
                organizationId,
                workflowId,
                notificationTask.Id,
                actorId,
                intakeCompleted.Version),
            cancellationToken);
        var notificationResult = await sender.Send(
            new CompleteTaskCommand(
                organizationId,
                workflowId,
                notificationTask.Id,
                actorId,
                intakeCompleted.Version + 1),
            cancellationToken);

        Assert.True(notificationResult.Succeeded);
        Assert.Empty(notificationResult.Failures);
        var completed = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);
        Assert.Equal(WorkflowTaskStatus.Completed, completed.Stages[1].Tasks[0].Status);
        Assert.Equal(WorkflowStageStatus.Completed, completed.Stages[1].Status);
        Assert.Equal(WorkflowStatus.Completed, completed.Status);
    }

    [Fact]
    public async Task Commands_persist_aggregate_operations_audit_and_outbox_atomically()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var organizationId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var gateId = Guid.Parse("47615de1-5058-4868-b94b-38a3ad54c4da");
        var sender = _fixture.Services.GetRequiredService<ISender>();
        var dbContext = _fixture.Services.GetRequiredService<WorkflowDbContext>();
        var workflowId = await sender.Send(new CreateWorkflowCommand(
            organizationId,
            claimId,
            WorkflowType.Primary,
            "property-claim-intake",
            1), cancellationToken);
        var created = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);

        Assert.Equal(WorkflowStatus.NotStarted, created.Status);
        Assert.Equal(1, created.Version);
        Assert.Equal(2, created.Stages.Count);
        Assert.Contains(
            created.Stages[0].Tasks[0].CompletionGates,
            gate => gate.SourceDefinitionId == gateId);

        await sender.Send(
            new StartWorkflowCommand(organizationId, workflowId, created.Version),
            cancellationToken);

        var started = await sender.Send(
            new GetWorkflowQuery(organizationId, workflowId),
            cancellationToken);
        var nextAction = await sender.Send(
            new GetWorkflowNextActionQuery(organizationId, workflowId),
            cancellationToken);
        var escalations = await sender.Send(
            new ListWorkflowEscalationsQuery(organizationId, workflowId),
            cancellationToken);
        var audits = await sender.Send(
            new GetWorkflowAuditQuery(organizationId, workflowId),
            cancellationToken);

        Assert.Equal(WorkflowStatus.Active, started.Status);
        Assert.Equal(2, started.Version);
        Assert.NotNull(nextAction);
        Assert.Equal(started.Stages[0].Tasks[0].Id, nextAction.TaskId);
        Assert.Equal("TASK_UNASSIGNED", nextAction.ReasonCode);
        Assert.Null(nextAction.OwnerId);
        Assert.Contains(
            escalations,
            escalation => escalation.Type == WorkflowEscalationType.TaskUnassigned);
        Assert.Equal(2, audits.Count);
        Assert.Contains(audits, audit => audit.Action == "CreateWorkflow");
        Assert.Contains(audits, audit => audit.Action == "StartWorkflow");

        dbContext.ChangeTracker.Clear();
        var persisted = await dbContext.Workflows
            .AsNoTracking()
            .AsSplitQuery()
            .SingleAsync(workflow => workflow.Id == workflowId, cancellationToken);
        var persistedGate = persisted.Stages
            .OrderBy(stage => stage.Order)
            .First()
            .Tasks
            .OrderBy(task => task.Order)
            .First()
            .CompletionGates
            .Single(gate => gate.SourceDefinitionId == gateId);
        var outboxMessages = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.AggregateId == workflowId)
            .OrderBy(message => message.OccurredAt)
            .ToListAsync(cancellationToken);

        Assert.Equal("PolicyNumber", persistedGate.Parameters["fieldName"]);
        Assert.Equal(2, outboxMessages.Count);
        Assert.All(outboxMessages, message => Assert.Null(message.ProcessedAt));
        Assert.All(outboxMessages, message => Assert.Equal(0, message.AttemptCount));
    }
}
