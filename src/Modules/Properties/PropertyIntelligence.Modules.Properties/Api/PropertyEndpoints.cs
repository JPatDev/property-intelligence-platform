using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Properties.Domain;
using PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Properties.Api;

internal static class PropertyEndpoints
{
    public static RouteGroupBuilder MapPropertyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/properties")
            .WithTags("Properties")
            .RequireAuthorization(PlatformPolicies.TenantAccess);

        group.MapPost(
                "/",
                async (
                    CreatePropertyRequest request,
                    IRequestIdentity identity,
                    PropertiesDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var address = ToDomain(request.Address);
                        var conflict = await FindIdentityConflictAsync(
                            dbContext,
                            identity.OrganizationId,
                            address.ToIdentityKey(),
                            request.ParcelNumber?.Trim().ToUpperInvariant(),
                            exceptPropertyId: null,
                            cancellationToken);
                        if (conflict is not null)
                        {
                            return conflict;
                        }

                        var property = Property.CreateProfile(
                            identity.OrganizationId,
                            address,
                            request.County,
                            request.ParcelNumber,
                            request.PropertyType,
                            request.YearBuilt,
                            request.SquareFeet,
                            request.RoofType,
                            request.RoofInstallationYear,
                            request.OwnerName,
                            request.OccupancyType,
                            request.Latitude,
                            request.Longitude,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        dbContext.Properties.Add(property);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Created(
                            $"/api/properties/{property.Id}",
                            ToResponse(property));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                    {
                        return IdentityConflict();
                    }
                })
            .WithName("CreateProperty")
            .RequireAuthorization(PlatformPolicies.ManageProperties)
            .Produces<PropertyResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapGet(
                "/{propertyId:guid}",
                async (
                    Guid propertyId,
                    IRequestIdentity identity,
                    PropertiesDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var property = await dbContext.Properties
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            candidate =>
                                candidate.OrganizationId == identity.OrganizationId &&
                                candidate.Id == propertyId,
                            cancellationToken);
                    return property is null
                        ? Results.NotFound()
                        : Results.Ok(ToResponse(property));
                })
            .WithName("GetProperty")
            .Produces<PropertyResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet(
                "/",
                async (
                    string? query,
                    int? limit,
                    IRequestIdentity identity,
                    PropertiesDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var normalizedQuery = query?.Trim();
                    if (string.IsNullOrEmpty(normalizedQuery) || normalizedQuery.Length < 2)
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            [nameof(query)] = ["A search query of at least two characters is required."],
                        });
                    }

                    var resultLimit = limit ?? 25;
                    if (resultLimit is < 1 or > 100)
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            [nameof(limit)] = ["The result limit must be between 1 and 100."],
                        });
                    }

                    var pattern = $"%{normalizedQuery}%";
                    var properties = await dbContext.Properties
                        .AsNoTracking()
                        .Where(property => property.OrganizationId == identity.OrganizationId)
                        .Where(property =>
                            EF.Functions.ILike(property.Address.Street, pattern) ||
                            EF.Functions.ILike(property.Address.City, pattern) ||
                            EF.Functions.ILike(property.Address.PostalCode, pattern) ||
                            EF.Functions.ILike(property.County, pattern) ||
                            (property.ParcelNumber != null &&
                             EF.Functions.ILike(property.ParcelNumber, pattern)) ||
                            (property.OwnerName != null &&
                             EF.Functions.ILike(property.OwnerName, pattern)))
                        .OrderBy(property => property.Address.State)
                        .ThenBy(property => property.Address.City)
                        .ThenBy(property => property.Address.Street)
                        .Take(resultLimit)
                        .Select(property => new PropertySummaryResponse(
                            property.Id,
                            new AddressResponse(
                                property.Address.Street,
                                property.Address.City,
                                property.Address.State,
                                property.Address.PostalCode),
                            property.County,
                            property.ParcelNumber,
                            property.PropertyType,
                            property.OwnerName))
                        .ToListAsync(cancellationToken);
                    return Results.Ok(properties);
                })
            .WithName("SearchProperties")
            .Produces<IReadOnlyCollection<PropertySummaryResponse>>()
            .ProducesValidationProblem();

        group.MapPut(
                "/{propertyId:guid}/profile",
                async (
                    Guid propertyId,
                    UpdatePropertyProfileRequest request,
                    IRequestIdentity identity,
                    PropertiesDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var property = await dbContext.Properties.SingleOrDefaultAsync(
                        candidate =>
                            candidate.OrganizationId == identity.OrganizationId &&
                            candidate.Id == propertyId,
                        cancellationToken);
                    if (property is null)
                    {
                        return Results.NotFound();
                    }

                    if (property.Version != request.ExpectedVersion)
                    {
                        return ConcurrencyConflict();
                    }

                    try
                    {
                        var address = ToDomain(request.Address);
                        var conflict = await FindIdentityConflictAsync(
                            dbContext,
                            identity.OrganizationId,
                            address.ToIdentityKey(),
                            request.ParcelNumber?.Trim().ToUpperInvariant(),
                            property.Id,
                            cancellationToken);
                        if (conflict is not null)
                        {
                            return conflict;
                        }

                        property.UpdateProfile(
                            address,
                            request.County,
                            request.ParcelNumber,
                            request.PropertyType,
                            request.YearBuilt,
                            request.SquareFeet,
                            request.RoofType,
                            request.RoofInstallationYear,
                            request.OwnerName,
                            request.OccupancyType,
                            request.Latitude,
                            request.Longitude,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Ok(ToResponse(property));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ConcurrencyConflict();
                    }
                    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                    {
                        return IdentityConflict();
                    }
                })
            .WithName("UpdatePropertyProfile")
            .RequireAuthorization(PlatformPolicies.ManageProperties)
            .Produces<PropertyResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult?> FindIdentityConflictAsync(
        PropertiesDbContext dbContext,
        Guid organizationId,
        string addressKey,
        string? parcelNumber,
        Guid? exceptPropertyId,
        CancellationToken cancellationToken)
    {
        var duplicateAddress = await dbContext.Properties.AsNoTracking().AnyAsync(
            property =>
                property.OrganizationId == organizationId &&
                property.Id != exceptPropertyId &&
                property.AddressKey == addressKey,
            cancellationToken);
        if (duplicateAddress)
        {
            return Results.Conflict(new
            {
                code = "property.address_exists",
                detail = "A property with this address already exists in the organization.",
            });
        }

        if (parcelNumber is null)
        {
            return null;
        }

        var duplicateParcel = await dbContext.Properties.AsNoTracking().AnyAsync(
            property =>
                property.OrganizationId == organizationId &&
                property.Id != exceptPropertyId &&
                property.ParcelNumber == parcelNumber,
            cancellationToken);
        return duplicateParcel
            ? Results.Conflict(new
            {
                code = "property.parcel_exists",
                detail = "A property with this parcel number already exists in the organization.",
            })
            : null;
    }

    private static PropertyResponse ToResponse(Property property) =>
        new(
            property.Id,
            property.OrganizationId,
            AddressResponse.From(property.Address),
            property.County,
            property.ParcelNumber,
            property.PropertyType,
            property.YearBuilt,
            property.SquareFeet,
            property.RoofType,
            property.RoofInstallationYear,
            property.OwnerName,
            property.OccupancyType,
            property.Latitude,
            property.Longitude,
            property.Version,
            property.CreatedAt,
            property.ModifiedAt);

    private static Address ToDomain(AddressRequest? request) =>
        request?.ToDomain()
        ?? throw new ArgumentException("A property address is required.", "address");

    private static IResult Validation(ArgumentException exception) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "request"] = [exception.Message],
        });

    private static IResult ConcurrencyConflict() =>
        Results.Conflict(new
        {
            code = "property.concurrency_conflict",
            detail = "The property was changed by another operation. Reload it and retry.",
        });

    private static IResult IdentityConflict() =>
        Results.Conflict(new
        {
            code = "property.identity_conflict",
            detail = "The address or parcel number is already assigned to another property.",
        });

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };
}

public sealed record AddressRequest(string Street, string City, string State, string PostalCode)
{
    internal Address ToDomain() => Address.Create(Street, City, State, PostalCode);
}

public sealed record CreatePropertyRequest(
    AddressRequest Address,
    string County,
    string? ParcelNumber,
    string? PropertyType,
    int? YearBuilt,
    int? SquareFeet,
    string? RoofType,
    int? RoofInstallationYear,
    string? OwnerName,
    string? OccupancyType,
    double? Latitude,
    double? Longitude);

public sealed record UpdatePropertyProfileRequest(
    AddressRequest Address,
    string County,
    string? ParcelNumber,
    string? PropertyType,
    int? YearBuilt,
    int? SquareFeet,
    string? RoofType,
    int? RoofInstallationYear,
    string? OwnerName,
    string? OccupancyType,
    double? Latitude,
    double? Longitude,
    long ExpectedVersion);

public sealed record AddressResponse(string Street, string City, string State, string PostalCode)
{
    internal static AddressResponse From(Address address) =>
        new(address.Street, address.City, address.State, address.PostalCode);
}

public sealed record PropertyResponse(
    Guid Id,
    Guid OrganizationId,
    AddressResponse Address,
    string County,
    string? ParcelNumber,
    string? PropertyType,
    int? YearBuilt,
    int? SquareFeet,
    string? RoofType,
    int? RoofInstallationYear,
    string? OwnerName,
    string? OccupancyType,
    double? Latitude,
    double? Longitude,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

public sealed record PropertySummaryResponse(
    Guid Id,
    AddressResponse Address,
    string County,
    string? ParcelNumber,
    string? PropertyType,
    string? OwnerName);
