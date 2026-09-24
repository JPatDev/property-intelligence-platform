using PropertyIntelligence.Modules.Properties.Domain;

namespace PropertyIntelligence.Modules.Properties.UnitTests;

public sealed class PropertyAggregateTests
{
    [Fact]
    public void Create_normalizes_identity_and_records_audit_values()
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

        var property = Property.Create(
            organizationId,
            Address.Create("  123   Main St  ", " Tampa ", "fl", "33602"),
            " Hillsborough ",
            " folio-42 ",
            actorId,
            createdAt);

        Assert.Equal(organizationId, property.OrganizationId);
        Assert.Equal("123 Main St", property.Address.Street);
        Assert.Equal("Tampa", property.Address.City);
        Assert.Equal("FL", property.Address.State);
        Assert.Equal("33602", property.Address.PostalCode);
        Assert.Equal("Hillsborough", property.County);
        Assert.Equal("FOLIO-42", property.ParcelNumber);
        Assert.Equal(actorId, property.CreatedBy);
        Assert.Equal(createdAt, property.CreatedAt);
        Assert.Equal(1, property.Version);
    }

    [Fact]
    public void Update_profile_sets_property_facts_and_postgis_coordinates()
    {
        var property = CreateProperty();
        var modifiedAt = new DateTimeOffset(2026, 8, 22, 13, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();

        property.UpdateProfile(
            Address.Create("500 Bayshore Blvd", "Tampa", "FL", "33606"),
            "Hillsborough",
            "folio-99",
            "Single Family",
            1985,
            2450,
            "Architectural Shingle",
            2020,
            "Casey Owner",
            "Owner Occupied",
            27.9441,
            -82.4637,
            actorId,
            modifiedAt);

        Assert.Equal("FOLIO-99", property.ParcelNumber);
        Assert.Equal(1985, property.YearBuilt);
        Assert.Equal(2450, property.SquareFeet);
        Assert.Equal(27.9441, property.Latitude);
        Assert.Equal(-82.4637, property.Longitude);
        Assert.Equal(4326, property.Location!.SRID);
        Assert.Equal(actorId, property.ModifiedBy);
        Assert.Equal(modifiedAt, property.ModifiedAt);
        Assert.Equal(2, property.Version);
    }

    [Fact]
    public void Create_profile_records_known_facts_without_creating_a_modification()
    {
        var property = Property.CreateProfile(
            Guid.NewGuid(),
            Address.Create("500 Bayshore Blvd", "Tampa", "FL", "33606"),
            "Hillsborough",
            "folio-99",
            "Single Family",
            1985,
            2450,
            "Architectural Shingle",
            2020,
            "Casey Owner",
            "Owner Occupied",
            27.9441,
            -82.4637,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal("Single Family", property.PropertyType);
        Assert.Equal(27.9441, property.Latitude);
        Assert.Equal(1, property.Version);
        Assert.Null(property.ModifiedAt);
        Assert.Null(property.ModifiedBy);
    }

    [Theory]
    [InlineData(91, -82)]
    [InlineData(27, -181)]
    public void Update_profile_rejects_invalid_coordinates(double latitude, double longitude)
    {
        var property = CreateProperty();

        Assert.Throws<ArgumentOutOfRangeException>(() => property.UpdateProfile(
            property.Address,
            property.County,
            property.ParcelNumber,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            latitude,
            longitude,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Address_rejects_an_invalid_state_or_postal_code()
    {
        Assert.Throws<ArgumentException>(() =>
            Address.Create("123 Main St", "Tampa", "Florida", "33602"));
        Assert.Throws<ArgumentException>(() =>
            Address.Create("123 Main St", "Tampa", "FL", "invalid"));
    }

    private static Property CreateProperty() =>
        Property.Create(
            Guid.NewGuid(),
            Address.Create("123 Main St", "Tampa", "FL", "33602"),
            "Hillsborough",
            null,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
}
