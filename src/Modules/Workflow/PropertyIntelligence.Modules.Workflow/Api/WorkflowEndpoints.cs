using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Workflow.Application.Commands;
using PropertyIntelligence.Modules.Workflow.Application.Definitions;
using PropertyIntelligence.Modules.Workflow.Application.Queries;

namespace PropertyIntelligence.Modules.Workflow.Api;

internal static class WorkflowEndpoints
{
    public static RouteGroupBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workflow")
            .WithTags("Workflow")
            .RequireAuthorization(PlatformPolicies.TenantAccess)
            .AddEndpointFilter<WorkflowExceptionFilter>();

        group.MapGet("/status", () => Results.Ok(new { module = "workflow", status = "ready" }));

        group.MapGet(
                "/definitions",
                async (
                    IRequestIdentity identity,
                    IWorkflowDefinitionCatalog catalog,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await catalog.ListAsync(
                        identity.OrganizationId,
                        cancellationToken)))
            .WithName("ListWorkflowDefinitions")
            .Produces<IReadOnlyList<WorkflowDefinitionSummary>>(StatusCodes.Status200OK);

        group.MapPost(
                "/claims/{claimId:guid}/workflows",
                async (
                    Guid claimId,
                    CreateWorkflowRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var workflowId = await sender.Send(
                        new CreateWorkflowCommand(
                            identity.OrganizationId,
                            claimId,
                            request.Type,
                            request.DefinitionKey,
                            request.DefinitionVersion),
                        cancellationToken);

                    return Results.Created(
                        $"/api/workflow/workflows/{workflowId}",
                        new { id = workflowId });
                })
            .WithName("CreateWorkflow")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet(
                "/workflows/{workflowId:guid}",
                async (
                    Guid workflowId,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new GetWorkflowQuery(identity.OrganizationId, workflowId),
                        cancellationToken)))
            .WithName("GetWorkflow")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(
                "/claims/{claimId:guid}/workflows",
                async (
                    Guid claimId,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new ListClaimWorkflowsQuery(identity.OrganizationId, claimId),
                        cancellationToken)))
            .WithName("ListClaimWorkflows")
            .Produces(StatusCodes.Status200OK);

        group.MapGet(
                "/workflows/{workflowId:guid}/next-action",
                async (
                    Guid workflowId,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var nextAction = await sender.Send(
                        new GetWorkflowNextActionQuery(identity.OrganizationId, workflowId),
                        cancellationToken);
                    return nextAction is null
                        ? Results.NoContent()
                        : Results.Ok(nextAction);
                })
            .WithName("GetWorkflowNextAction")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet(
                "/workflows/{workflowId:guid}/audit",
                async (
                    Guid workflowId,
                    int? limit,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new GetWorkflowAuditQuery(
                            identity.OrganizationId,
                            workflowId,
                            limit ?? 100),
                        cancellationToken)))
            .WithName("GetWorkflowAudit")
            .RequireAuthorization(PlatformPolicies.ViewWorkflowAudit)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet(
                "/escalations",
                async (
                    Guid? workflowId,
                    bool includeResolved,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new ListWorkflowEscalationsQuery(
                            identity.OrganizationId,
                            workflowId,
                            includeResolved),
                        cancellationToken)))
            .WithName("ListWorkflowEscalations")
            .Produces(StatusCodes.Status200OK);

        group.MapPost(
                "/escalations/{escalationId:guid}/acknowledge",
                async (
                    Guid escalationId,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new AcknowledgeWorkflowEscalationCommand(
                            identity.OrganizationId,
                            escalationId,
                            identity.UserId),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("AcknowledgeWorkflowEscalation")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(
                "/workflows/{workflowId:guid}/start",
                async (
                    Guid workflowId,
                    VersionedWorkflowRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new StartWorkflowCommand(identity.OrganizationId, workflowId, request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("StartWorkflow")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/assign",
                async (
                    Guid workflowId,
                    Guid taskId,
                    AssignTaskRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new AssignTaskCommand(
                            identity.OrganizationId,
                            workflowId,
                            taskId,
                            request.AssigneeId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("AssignWorkflowTask")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/start",
                async (
                    Guid workflowId,
                    Guid taskId,
                    VersionedWorkflowRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new StartTaskCommand(
                            identity.OrganizationId,
                            workflowId,
                            taskId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("StartWorkflowTask")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/complete",
                async (
                    Guid workflowId,
                    Guid taskId,
                    CompleteTaskRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    // Gate results are intentionally not accepted from clients.
                    // The command evaluates authoritative evidence through registered handlers.
                    var result = await sender.Send(
                        new CompleteTaskCommand(
                            identity.OrganizationId,
                            workflowId,
                            taskId,
                            identity.UserId,
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
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/completion-readiness",
                async (
                    Guid workflowId,
                    Guid taskId,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await sender.Send(
                        new GetTaskCompletionReadinessQuery(
                            identity.OrganizationId,
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
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var blockerId = await sender.Send(
                        new AddTaskBlockerCommand(
                            identity.OrganizationId,
                            workflowId,
                            taskId,
                            request.Code,
                            request.Description,
                            identity.UserId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.Created(
                        $"/api/workflow/workflows/{workflowId}/tasks/{taskId}/blockers/{blockerId}",
                        new { id = blockerId });
                })
            .WithName("AddWorkflowTaskBlocker")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status201Created);

        group.MapPost(
                "/workflows/{workflowId:guid}/tasks/{taskId:guid}/blockers/{blockerId:guid}/resolve",
                async (
                    Guid workflowId,
                    Guid taskId,
                    Guid blockerId,
                    ResolveTaskBlockerRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new ResolveTaskBlockerCommand(
                            identity.OrganizationId,
                            workflowId,
                            taskId,
                            blockerId,
                            identity.UserId,
                            request.Reason,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("ResolveWorkflowTaskBlocker")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/stages/{stageId:guid}/skip",
                async (
                    Guid workflowId,
                    Guid stageId,
                    SkipStageRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new SkipStageCommand(
                            identity.OrganizationId,
                            workflowId,
                            stageId,
                            identity.UserId,
                            request.Reason,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SkipWorkflowStage")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/cancel",
                async (
                    Guid workflowId,
                    CancelWorkflowRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new CancelWorkflowCommand(
                            identity.OrganizationId,
                            workflowId,
                            identity.UserId,
                            request.Reason,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("CancelWorkflow")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost(
                "/workflows/{workflowId:guid}/archive",
                async (
                    Guid workflowId,
                    VersionedWorkflowRequest request,
                    IRequestIdentity identity,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(
                        new ArchiveWorkflowCommand(
                            identity.OrganizationId,
                            workflowId,
                            request.ExpectedVersion),
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithName("ArchiveWorkflow")
            .RequireAuthorization(PlatformPolicies.ManageWorkflow)
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }
}
