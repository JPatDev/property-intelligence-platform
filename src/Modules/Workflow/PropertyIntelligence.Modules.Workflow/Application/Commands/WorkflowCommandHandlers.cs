using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Application.Errors;
using PropertyIntelligence.Modules.Workflow.Application.Definitions;
using PropertyIntelligence.Modules.Workflow.Application.Gates;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Workflow.Application.Commands;

internal sealed class CreateWorkflowCommandHandler(
    WorkflowDbContext dbContext,
    IWorkflowDefinitionCatalog definitionCatalog,
    TimeProvider timeProvider)
    : IRequestHandler<CreateWorkflowCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var definition = await definitionCatalog.FindAsync(
            request.OrganizationId,
            request.DefinitionKey,
            request.DefinitionVersion,
            cancellationToken)
            ?? throw new WorkflowDefinitionNotFoundException(
                request.DefinitionKey,
                request.DefinitionVersion);

        if (definition.Type != request.Type)
        {
            throw new WorkflowDefinitionTypeMismatchException(
                request.DefinitionKey,
                request.DefinitionVersion,
                request.Type,
                definition.Type);
        }

        var workflow = WorkflowInstance.Create(
            request.OrganizationId,
            request.ClaimId,
            request.Type,
            definition.Snapshot,
            timeProvider.GetUtcNow());

        dbContext.Workflows.Add(workflow);
        return workflow.Id;
    }
}

internal sealed class StartWorkflowCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<StartWorkflowCommand, Unit>
{
    public async Task<Unit> Handle(StartWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.Start(timeProvider.GetUtcNow());
        return Unit.Value;
    }
}

internal sealed class AssignTaskCommandHandler(WorkflowDbContext dbContext)
    : IRequestHandler<AssignTaskCommand, Unit>
{
    public async Task<Unit> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.AssignTask(request.TaskId, request.AssigneeId);
        return Unit.Value;
    }
}

internal sealed class StartTaskCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<StartTaskCommand, Unit>
{
    public async Task<Unit> Handle(StartTaskCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.StartTask(request.TaskId, timeProvider.GetUtcNow());
        return Unit.Value;
    }
}

internal sealed class CompleteTaskCommandHandler(
    WorkflowDbContext dbContext,
    ICompletionGateEvaluator gateEvaluator,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteTaskCommand, TaskCompletionResult>
{
    public async Task<TaskCompletionResult> Handle(
        CompleteTaskCommand request,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        var task = workflow.GetTask(request.TaskId);
        var evaluations = await gateEvaluator.EvaluateAsync(
            workflow,
            task,
            cancellationToken);
        return workflow.CompleteTask(
            request.TaskId,
            request.ActorId,
            timeProvider.GetUtcNow(),
            evaluations);
    }
}

internal sealed class AddTaskBlockerCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<AddTaskBlockerCommand, Guid>
{
    public async Task<Guid> Handle(AddTaskBlockerCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        return workflow.AddTaskBlocker(
            request.TaskId,
            request.Code,
            request.Description,
            request.ActorId,
            timeProvider.GetUtcNow());
    }
}

internal sealed class ResolveTaskBlockerCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<ResolveTaskBlockerCommand, Unit>
{
    public async Task<Unit> Handle(
        ResolveTaskBlockerCommand request,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.ResolveTaskBlocker(
            request.TaskId,
            request.BlockerId,
            request.ActorId,
            request.Reason,
            timeProvider.GetUtcNow());
        return Unit.Value;
    }
}

internal sealed class SkipStageCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<SkipStageCommand, Unit>
{
    public async Task<Unit> Handle(SkipStageCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.SkipStage(
            request.StageId,
            request.ActorId,
            request.Reason,
            timeProvider.GetUtcNow());
        return Unit.Value;
    }
}

internal sealed class CancelWorkflowCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<CancelWorkflowCommand, Unit>
{
    public async Task<Unit> Handle(CancelWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.Cancel(request.ActorId, request.Reason, timeProvider.GetUtcNow());
        return Unit.Value;
    }
}

internal sealed class ArchiveWorkflowCommandHandler(
    WorkflowDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<ArchiveWorkflowCommand, Unit>
{
    public async Task<Unit> Handle(ArchiveWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.GetWorkflowAsync(
            request.OrganizationId,
            request.WorkflowId,
            request.ExpectedVersion,
            cancellationToken);
        workflow.Archive(timeProvider.GetUtcNow());
        return Unit.Value;
    }
}

internal static class WorkflowDbContextCommandExtensions
{
    public static async Task<WorkflowInstance> GetWorkflowAsync(
        this WorkflowDbContext dbContext,
        Guid organizationId,
        Guid workflowId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.OrganizationId == organizationId && candidate.Id == workflowId,
                cancellationToken)
            ?? throw new WorkflowNotFoundException(workflowId);

        if (workflow.Version != expectedVersion)
        {
            throw new WorkflowConcurrencyException();
        }

        return workflow;
    }
}
