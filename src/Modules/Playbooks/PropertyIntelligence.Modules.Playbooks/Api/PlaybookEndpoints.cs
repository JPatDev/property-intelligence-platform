using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Playbooks.Contracts;
using PropertyIntelligence.Modules.Playbooks.Application.Assignment;
using PropertyIntelligence.Modules.Playbooks.Domain;
using PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Playbooks.Api;

internal static class PlaybookEndpoints
{
    public static RouteGroupBuilder MapPlaybookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/playbooks")
            .WithTags("Playbooks")
            .RequireAuthorization(PlatformPolicies.TenantAccess);

        group.MapGet(
                "/published",
                async (
                    IRequestIdentity identity,
                    IPublishedPlaybookReader reader,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await reader.ListAsync(identity.OrganizationId, cancellationToken)))
            .WithName("ListPublishedPlaybooks");

        group.MapPost(
                "/recommendations",
                async (
                    RecommendPlaybooksRequest request,
                    IRequestIdentity identity,
                    PlaybookAssignmentRecommendationService service,
                    CancellationToken cancellationToken) =>
                {
                    var recommendations = await service.RecommendAsync(
                        identity.OrganizationId,
                        request.ClaimId,
                        identity.UserId,
                        cancellationToken);
                    return recommendations is null
                        ? Results.NotFound(new
                        {
                            code = "playbook.claim_not_found",
                            detail = "The tenant-scoped claim was not found.",
                        })
                        : Results.Ok(recommendations);
                })
            .WithName("RecommendPlaybooks");

        group.MapPost(
                "/",
                async (
                    CreatePlaybookRequest request,
                    IRequestIdentity identity,
                    PlaybooksDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var playbook = Playbook.Create(
                            identity.OrganizationId,
                            request.Key,
                            request.Name,
                            request.Description,
                            request.WorkflowType,
                            request.SchemaVersion,
                            request.Stages,
                            identity.UserId,
                            timeProvider.GetUtcNow(),
                            request.AssignmentRules);
                        dbContext.Playbooks.Add(playbook);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Created(
                            $"/api/playbooks/{playbook.Id}",
                            ToResponse(playbook));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (DbUpdateException)
                    {
                        return Results.Conflict(new
                        {
                            code = "playbook.key_conflict",
                            detail = "A playbook with this key already exists for the organization.",
                        });
                    }
                })
            .WithName("CreatePlaybook")
            .RequireAuthorization(PlatformPolicies.ManagePlaybooks);

        group.MapGet(
                "/{playbookId:guid}",
                async (
                    Guid playbookId,
                    IRequestIdentity identity,
                    PlaybooksDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var playbook = await Query(dbContext)
                        .SingleOrDefaultAsync(candidate =>
                            candidate.OrganizationId == identity.OrganizationId &&
                            candidate.Id == playbookId,
                            cancellationToken);
                    return playbook is null
                        ? Results.NotFound()
                        : Results.Ok(ToResponse(playbook));
                })
            .WithName("GetPlaybook");

        group.MapPut(
                "/{playbookId:guid}/versions/{versionId:guid}",
                async (
                    Guid playbookId,
                    Guid versionId,
                    UpdatePlaybookDraftRequest request,
                    IRequestIdentity identity,
                    PlaybooksDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var playbook = await FindForUpdate(
                        dbContext,
                        identity.OrganizationId,
                        playbookId,
                        cancellationToken);
                    if (playbook is null)
                    {
                        return Results.NotFound();
                    }

                    if (playbook.Version != request.ExpectedVersion)
                    {
                        return ConcurrencyConflict();
                    }

                    try
                    {
                        playbook.UpdateDraft(
                            versionId,
                            request.Name,
                            request.Description,
                            request.WorkflowType,
                            request.SchemaVersion,
                            request.Stages,
                            identity.UserId,
                            timeProvider.GetUtcNow(),
                            request.AssignmentRules);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Ok(ToResponse(playbook));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return TransitionConflict(exception);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ConcurrencyConflict();
                    }
                })
            .WithName("UpdatePlaybookDraft")
            .RequireAuthorization(PlatformPolicies.ManagePlaybooks);

        group.MapPost(
                "/{playbookId:guid}/versions/{versionId:guid}/publish",
                async (
                    Guid playbookId,
                    Guid versionId,
                    VersionedPlaybookRequest request,
                    IRequestIdentity identity,
                    PlaybooksDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var playbook = await FindForUpdate(
                        dbContext,
                        identity.OrganizationId,
                        playbookId,
                        cancellationToken);
                    if (playbook is null)
                    {
                        return Results.NotFound();
                    }

                    if (playbook.Version != request.ExpectedVersion)
                    {
                        return ConcurrencyConflict();
                    }

                    try
                    {
                        playbook.Publish(versionId, identity.UserId, timeProvider.GetUtcNow());
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Ok(ToResponse(playbook));
                    }
                    catch (InvalidOperationException exception)
                    {
                        return TransitionConflict(exception);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ConcurrencyConflict();
                    }
                })
            .WithName("PublishPlaybookVersion")
            .RequireAuthorization(PlatformPolicies.ManagePlaybooks);

        group.MapPost(
                "/{playbookId:guid}/drafts",
                async (
                    Guid playbookId,
                    VersionedPlaybookRequest request,
                    IRequestIdentity identity,
                    PlaybooksDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var playbook = await FindForUpdate(
                        dbContext,
                        identity.OrganizationId,
                        playbookId,
                        cancellationToken);
                    if (playbook is null)
                    {
                        return Results.NotFound();
                    }

                    if (playbook.Version != request.ExpectedVersion)
                    {
                        return ConcurrencyConflict();
                    }

                    try
                    {
                        var versionId = playbook.CreateDraft(
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Created(
                            $"/api/playbooks/{playbook.Id}/versions/{versionId}",
                            ToResponse(playbook));
                    }
                    catch (InvalidOperationException exception)
                    {
                        return TransitionConflict(exception);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ConcurrencyConflict();
                    }
                })
            .WithName("CreatePlaybookDraft")
            .RequireAuthorization(PlatformPolicies.ManagePlaybooks);

        return group;
    }

    private static IQueryable<Playbook> Query(PlaybooksDbContext dbContext) =>
        dbContext.Playbooks.AsNoTracking().Include(playbook => playbook.Versions);

    private static Task<Playbook?> FindForUpdate(
        PlaybooksDbContext dbContext,
        Guid organizationId,
        Guid playbookId,
        CancellationToken cancellationToken) =>
        dbContext.Playbooks
            .Include(playbook => playbook.Versions)
            .SingleOrDefaultAsync(playbook =>
                playbook.OrganizationId == organizationId &&
                playbook.Id == playbookId,
                cancellationToken);

    private static PlaybookResponse ToResponse(Playbook playbook) =>
        new(
            playbook.Id,
            playbook.OrganizationId,
            playbook.Key,
            playbook.Name,
            playbook.Description,
            playbook.Status,
            playbook.Version,
            playbook.Versions
                .OrderByDescending(version => version.Version)
                .Select(version => new PlaybookVersionResponse(
                    version.Id,
                    version.Version,
                    version.WorkflowType,
                    version.SchemaVersion,
                    version.Status,
                    version.CreatedAt,
                    version.PublishedAt,
                    version.Stages,
                    version.AssignmentRules))
                .ToArray());

    private static IResult Validation(ArgumentException exception) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "request"] = [exception.Message],
        });

    private static IResult ConcurrencyConflict() =>
        Results.Conflict(new
        {
            code = "playbook.concurrency_conflict",
            detail = "The playbook changed. Reload it and retry.",
        });

    private static IResult TransitionConflict(Exception exception) =>
        Results.Conflict(new
        {
            code = "playbook.transition_invalid",
            detail = exception.Message,
        });
}

public sealed record CreatePlaybookRequest(
    string Key,
    string Name,
    string Description,
    string WorkflowType,
    int SchemaVersion,
    IReadOnlyList<PlaybookStageDefinition> Stages,
    IReadOnlyList<PlaybookAssignmentRuleDefinition>? AssignmentRules = null);

public sealed record UpdatePlaybookDraftRequest(
    string Name,
    string Description,
    string WorkflowType,
    int SchemaVersion,
    IReadOnlyList<PlaybookStageDefinition> Stages,
    long ExpectedVersion,
    IReadOnlyList<PlaybookAssignmentRuleDefinition>? AssignmentRules = null);

public sealed record RecommendPlaybooksRequest(Guid ClaimId);

public sealed record VersionedPlaybookRequest(long ExpectedVersion);

public sealed record PlaybookResponse(
    Guid Id,
    Guid OrganizationId,
    string Key,
    string Name,
    string Description,
    PlaybookStatus Status,
    long Version,
    IReadOnlyList<PlaybookVersionResponse> Versions);

public sealed record PlaybookVersionResponse(
    Guid Id,
    int Version,
    string WorkflowType,
    int SchemaVersion,
    PlaybookVersionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<PlaybookStageDefinition> Stages,
    IReadOnlyList<PlaybookAssignmentRuleDefinition> AssignmentRules);
