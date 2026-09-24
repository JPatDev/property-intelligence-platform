using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.Modules.Properties.Contracts;
using PropertyIntelligence.Modules.Properties.Domain;
using PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Properties.IntegrationTests;

public sealed class PropertyPersistenceTests(PostgreSqlPropertiesFixture fixture)
    : IClassFixture<PostgreSqlPropertiesFixture>
{
    [Fact]
    public async Task Property_profile_and_location_round_trip_through_postgresql()
    {
        Assert.SkipUnless(fixture.IsAvailable, fixture.UnavailableReason);
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbContext = fixture.Services.GetRequiredService<PropertiesDbContext>();
        var property = CreateProperty(Guid.NewGuid(), "123 Main St", "PARCEL-1");
        property.UpdateProfile(
            property.Address,
            property.County,
            property.ParcelNumber,
            "Single Family",
            1995,
            2100,
            "Metal",
            2021,
            "Alex Owner",
            "Owner Occupied",
            27.9506,
            -82.4572,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var reloaded = await dbContext.Properties.SingleAsync(
            candidate => candidate.Id == property.Id,
            cancellationToken);

        Assert.Equal("123 Main St", reloaded.Address.Street);
        Assert.Equal("PARCEL-1", reloaded.ParcelNumber);
        Assert.Equal(27.9506, reloaded.Latitude);
        Assert.Equal(-82.4572, reloaded.Longitude);
        Assert.Equal(4326, reloaded.Location!.SRID);
    }

    [Fact]
    public async Task Property_directory_enforces_the_organization_boundary()
    {
        Assert.SkipUnless(fixture.IsAvailable, fixture.UnavailableReason);
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbContext = fixture.Services.GetRequiredService<PropertiesDbContext>();
        var organizationId = Guid.NewGuid();
        var property = CreateProperty(organizationId, "200 Oak Ave", "PARCEL-2");
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        var directory = fixture.Services.GetRequiredService<IPropertyDirectory>();

        Assert.NotNull(await directory.FindAsync(organizationId, property.Id, cancellationToken));
        Assert.Null(await directory.FindAsync(Guid.NewGuid(), property.Id, cancellationToken));
    }

    [Fact]
    public async Task Address_identity_is_unique_within_an_organization()
    {
        Assert.SkipUnless(fixture.IsAvailable, fixture.UnavailableReason);
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbContext = fixture.Services.GetRequiredService<PropertiesDbContext>();
        var organizationId = Guid.NewGuid();
        dbContext.Properties.Add(CreateProperty(organizationId, "300 Pine St", null));
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
        dbContext.Properties.Add(CreateProperty(organizationId, "300 Pine St", null));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync(cancellationToken));
        dbContext.ChangeTracker.Clear();
    }

    private static Property CreateProperty(
        Guid organizationId,
        string street,
        string? parcelNumber) =>
        Property.Create(
            organizationId,
            Address.Create(street, "Tampa", "FL", "33602"),
            "Hillsborough",
            parcelNumber,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
}
