using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PropertyIntelligence.Modules.Workflow.Application.Commands;
using PropertyIntelligence.Modules.Workflow.Application.Queries;

namespace PropertyIntelligence.Modules.Workflow.Api;

internal static class WorkflowEndpoints
{
    private const string OrganizationHeader = "X-Organization-Id";

    public static RouteGroupBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workflow")
            .WithTags("Workflow")
            .AddEndpointFilter<WorkflowExceptionFilter>();

        group.MapGet("/status", () => Results.Ok(new { module = "workflow", status = "ready" }));

        group.MapPost(
                "/claims/{claimId:guid}/workflows",
                async (
                    Guid claimId,
                    CreateWorkflowRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var workflowId = await sender.Send(
                        new CreateWorkflowCommand(
                            organizationId,
                            claimId,
                            request.Type,
                            request.ToSnapshot()),
                        cancellationToken);

                    return Results.Created(
                        $"/api/workflow/workflows/{workflowId}",
                        new { id = workflowId });
                })
            .WithName("CreateWorkflow")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet(
                "/workflows/{workflowId:guid}",
                async (
                    Guid workflowId,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new GetWorkflowQuery(organizationId, workflowId),
                        cancellationToken)))
            .WithName("GetWorkflow")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(
                "/claims/{claimId:guid}/workflows",
                async (
                    Guid claimId,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new ListClaimWorkflowsQuery(organizationId, claimId),
                        cancellationToken)))
            .WithName("ListClaimWorkflows")
            .Produces(StatusCodes.Status200OK);

        group.MapGet(
                "/workflows/{workflowId:guid}/next-action",
                async (
                    Guid workflowId,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var nextAction = await sender.Send(
                        new GetWorkflowNextActionQuery(organizationId, workflowId),
                        cancellationToken);
                    return nextAction is null
                        ? Results.NoContent()
                        : Results.Ok(nextAction);
                })
            .WithName("GetWorkflowNextAction")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet(
                "/escalations",
                async (
                    Guid? workflowId,
                    bool includeResolved,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new ListWorkflowEscalationsQuery(
                            organizationId,
                            workflowId,
                            includeResolved),
                        cancellationToken)))
            .WithName("ListWorkflowEscalations")
            .Produces(StatusCodes.Status200OK);

        group.MapPost(
                "/escalations/{escalationId:guid}/acknowledge",
                async (
                    Guid escalationId,
                    AcknowledgeEscalationRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new AcknowledgeWorkflowEscalationCommand(
                            organizationId,
                            escalationId,
                            request.ActorId),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("AcknowledgeWorkflowEscalation")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(
                "/workflows/{workflowId:guid}/start",
                async (
                    Guid workflowId,
                    VersionedWorkflowRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new StartWorkflowCommand(organizationId, workflowId, request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("StartWorkflow")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/assign",
                async (
                    Guid workflowId,
                    Guid taskId,
                    AssignTaskRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new AssignTaskCommand(
                            organizationId,
                            workflowId,
                            taskId,
                            request.AssigneeId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("AssignWorkflowTask")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/start",
                async (
                    Guid workflowId,
                    Guid taskId,
                    VersionedWorkflowRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new StartTaskCommand(
                            organizationId,
                            workflowId,
                            taskId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("StartWorkflowTask")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/complete",
                async (
                    Guid workflowId,
                    Guid taskId,
                    CompleteTaskRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    // Gate results are intentionally not accepted from clients.
                    // The command evaluates authoritative evidence through registered handlers.
                    var result = await sender.Send(
                        new CompleteTaskCommand(
                            organizationId,
                            workflowId,
                            taskId,
                            request.ActorId,
                            request.ExpectedVersion),
                        cancellationToken);

                    return result.Succeeded
                        ? Results.NoContent()
                        : Results.Conflict(new
                        {
                            code = "workflow.completion_gates_failed",
                            failures = result.Failures,
                        });
                })
            .WithName("CompleteWorkflowTask")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/completion-readiness",
                async (
                    Guid workflowId,
                    Guid taskId,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new GetTaskCompletionReadinessQuery(
                            organizationId,
                            workflowId,
                            taskId),
                        cancellationToken)))
            .WithName("GetWorkflowTaskCompletionReadiness")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/blockers",
                async (
                    Guid workflowId,
                    Guid taskId,
                    AddTaskBlockerRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var blockerId = await sender.Send(
                        new AddTaskBlockerCommand(
                            organizationId,
                            workflowId,
                            taskId,
                            request.Code,
                            request.Description,
                            request.ActorId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.Created(
                        $"/api/workflow/workflows/{workflowId}/tasks/{taskId}/blockers/{blockerId}",
                        new { id = blockerId });
                })
            .WithName("AddWorkflowTaskBlocker")
            .Produces(StatusCodes.Status201Created);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/blockers/{blockerId:guid}/resolve",
                async (
                    Guid workflowId,
                    Guid taskId,
                    Guid blockerId,
                    ResolveTaskBlockerRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new ResolveTaskBlockerCommand(
                            organizationId,
                            workflowId,
                            taskId,
                            blockerId,
                            request.ActorId,
                            request.Reason,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("ResolveWorkflowTaskBlocker")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/stages/{stageId:guid}/skip",
                async (
                    Guid workflowId,
                    Guid stageId,
                    SkipStageRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new SkipStageCommand(
                            organizationId,
                            workflowId,
                            stageId,
                            request.ActorId,
                            request.Reason,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SkipWorkflowStage")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/cancel",
                async (
                    Guid workflowId,
                    CancelWorkflowRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new CancelWorkflowCommand(
                            organizationId,
                            workflowId,
                            request.ActorId,
                            request.Reason,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("CancelWorkflow")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/archive",
                async (
                    Guid workflowId,
                    VersionedWorkflowRequest request,
                    [FromHeader(Name = OrganizationHeader)] Guid organizationId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new ArchiveWorkflowCommand(
                            organizationId,
                            workflowId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("ArchiveWorkflow")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }
}
