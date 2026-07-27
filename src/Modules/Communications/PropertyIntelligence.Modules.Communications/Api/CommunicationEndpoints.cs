using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Communications.Domain;
using PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Communications.Api;

internal static class CommunicationEndpoints
{
    public static RouteGroupBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/communications")
            .WithTags("Communications")
            .RequireAuthorization(PlatformPolicies.TenantAccess);

        group.MapPost(
                "/",
                async (
                    CreateCommunicationRequest request,
                    IRequestIdentity identity,
                    CommunicationsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var communication = CommunicationRecord.Create(
                            identity.OrganizationId,
                            request.ClaimId,
                            request.CommunicationType,
                            request.Direction,
                            request.Channel,
                            request.Subject,
                            request.Recipient,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        dbContext.Communications.Add(communication);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Created(
                            $"/api/communications/{communication.Id}",
                            ToResponse(communication));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                })
            .WithName("CreateCommunication")
            .RequireAuthorization(PlatformPolicies.ManageCommunications)
            .Produces<CommunicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet(
                "/{communicationId:guid}",
                async (
                    Guid communicationId,
                    IRequestIdentity identity,
                    CommunicationsDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var communication = await dbContext.Communications
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            candidate =>
                                candidate.OrganizationId == identity.OrganizationId &&
                                candidate.Id == communicationId,
                            cancellationToken);
                    return communication is null
                        ? Results.NotFound()
                        : Results.Ok(ToResponse(communication));
                })
            .WithName("GetCommunication")
            .Produces<CommunicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut(
                "/{communicationId:guid}/status",
                async (
                    Guid communicationId,
                    ChangeCommunicationStatusRequest request,
                    IRequestIdentity identity,
                    CommunicationsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var communication = await dbContext.Communications.SingleOrDefaultAsync(
                        candidate =>
                            candidate.OrganizationId == identity.OrganizationId &&
                            candidate.Id == communicationId,
                        cancellationToken);
                    if (communication is null)
                    {
                        return Results.NotFound();
                    }

                    if (communication.Version != request.ExpectedVersion)
                    {
                        return Conflict();
                    }

                    try
                    {
                        communication.ChangeStatus(
                            request.Status,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Ok(ToResponse(communication));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Conflict(new
                        {
                            code = "communication.transition_invalid",
                            detail = exception.Message,
                        });
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return Conflict();
                    }
                })
            .WithName("ChangeCommunicationStatus")
            .RequireAuthorization(PlatformPolicies.ManageCommunications)
            .Produces<CommunicationResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static CommunicationResponse ToResponse(CommunicationRecord communication) =>
        new(
            communication.Id,
            communication.OrganizationId,
            communication.ClaimId,
            communication.CommunicationType,
            communication.Direction,
            communication.Channel,
            communication.Subject,
            communication.Recipient,
            communication.Status,
            communication.SentAt,
            communication.Version,
            communication.CreatedAt,
            communication.ModifiedAt);

    private static IResult Validation(ArgumentException exception) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "request"] = [exception.Message],
        });

    private static IResult Conflict() =>
        Results.Conflict(new
        {
            code = "communication.concurrency_conflict",
            detail = "The communication was changed by another operation. Reload it and retry.",
        });
}

public sealed record CreateCommunicationRequest(
    Guid ClaimId,
    string CommunicationType,
    CommunicationDirection Direction,
    CommunicationChannel Channel,
    string Subject,
    string Recipient);

public sealed record ChangeCommunicationStatusRequest(
    CommunicationStatus Status,
    long ExpectedVersion);

public sealed record CommunicationResponse(
    Guid Id,
    Guid OrganizationId,
    Guid ClaimId,
    string CommunicationType,
    CommunicationDirection Direction,
    CommunicationChannel Channel,
    string Subject,
    string Recipient,
    CommunicationStatus Status,
    DateTimeOffset? SentAt,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);
