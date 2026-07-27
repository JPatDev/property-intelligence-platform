using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Claims.Domain;
using PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Claims.Api;

internal static class ClaimEndpoints
{
    public static RouteGroupBuilder MapClaimEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/claims")
            .WithTags("Claims")
            .RequireAuthorization(PlatformPolicies.TenantAccess);

        group.MapPost(
                "/",
                async (
                    CreateClaimRequest request,
                    IRequestIdentity identity,
                    ClaimsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var claim = Claim.Create(
                            identity.OrganizationId,
                            request.PropertyId,
                            request.ClaimNumber,
                            request.PolicyNumber,
                            request.DateOfLoss,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        dbContext.Claims.Add(claim);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Created($"/api/claims/{claim.Id}", ToResponse(claim));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                })
            .WithName("CreateClaim")
            .RequireAuthorization(PlatformPolicies.ManageClaims)
            .Produces<ClaimResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet(
                "/{claimId:guid}",
                async (
                    Guid claimId,
                    IRequestIdentity identity,
                    ClaimsDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var claim = await dbContext.Claims
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            candidate =>
                                candidate.OrganizationId == identity.OrganizationId &&
                                candidate.Id == claimId,
                            cancellationToken);
                    return claim is null ? Results.NotFound() : Results.Ok(ToResponse(claim));
                })
            .WithName("GetClaim")
            .Produces<ClaimResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut(
                "/{claimId:guid}/facts",
                async (
                    Guid claimId,
                    UpdateClaimFactsRequest request,
                    IRequestIdentity identity,
                    ClaimsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var claim = await dbContext.Claims.SingleOrDefaultAsync(
                        candidate =>
                            candidate.OrganizationId == identity.OrganizationId &&
                            candidate.Id == claimId,
                        cancellationToken);
                    if (claim is null)
                    {
                        return Results.NotFound();
                    }

                    if (claim.Version != request.ExpectedVersion)
                    {
                        return Results.Conflict(new
                        {
                            code = "claim.concurrency_conflict",
                            detail = "The claim was changed by another operation. Reload it and retry.",
                        });
                    }

                    try
                    {
                        claim.UpdateFacts(
                            request.PolicyNumber,
                            request.DateOfLoss,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Ok(ToResponse(claim));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return Results.Conflict(new
                        {
                            code = "claim.concurrency_conflict",
                            detail = "The claim was changed by another operation. Reload it and retry.",
                        });
                    }
                })
            .WithName("UpdateClaimFacts")
            .RequireAuthorization(PlatformPolicies.ManageClaims)
            .Produces<ClaimResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static ClaimResponse ToResponse(Claim claim) =>
        new(
            claim.Id,
            claim.OrganizationId,
            claim.PropertyId,
            claim.ClaimNumber,
            claim.PolicyNumber,
            claim.DateOfLoss,
            claim.Status,
            claim.Version,
            claim.CreatedAt,
            claim.ModifiedAt);

    private static IResult Validation(ArgumentException exception) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "request"] = [exception.Message],
        });
}

public sealed record CreateClaimRequest(
    Guid PropertyId,
    string ClaimNumber,
    string? PolicyNumber,
    DateOnly? DateOfLoss);

public sealed record UpdateClaimFactsRequest(
    string? PolicyNumber,
    DateOnly? DateOfLoss,
    long ExpectedVersion);

public sealed record ClaimResponse(
    Guid Id,
    Guid OrganizationId,
    Guid PropertyId,
    string ClaimNumber,
    string? PolicyNumber,
    DateOnly? DateOfLoss,
    ClaimStatus Status,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);
