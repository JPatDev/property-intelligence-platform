using NetTopologySuite.Geometries;

namespace PropertyIntelligence.Modules.Properties.Domain;

public sealed class Property
{
    private Property()
    {
    }

    private Property(
        Guid id,
        Guid organizationId,
        Address address,
        string county,
        string? parcelNumber,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = RequireId(organizationId, "organization");
        Address = address ?? throw new ArgumentNullException(nameof(address));
        AddressKey = address.ToIdentityKey();
        County = NormalizeRequired(county, "county", 100);
        ParcelNumber = NormalizeOptional(parcelNumber, 100, upperCase: true);
        CreatedBy = RequireId(createdBy, "actor");
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Address Address { get; private set; } = null!;
    public string AddressKey { get; private set; } = string.Empty;
    public string County { get; private set; } = string.Empty;
    public string? ParcelNumber { get; private set; }
    public string? PropertyType { get; private set; }
    public int? YearBuilt { get; private set; }
    public int? SquareFeet { get; private set; }
    public string? RoofType { get; private set; }
    public int? RoofInstallationYear { get; private set; }
    public string? OwnerName { get; private set; }
    public string? OccupancyType { get; private set; }
    public Point? Location { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public long Version { get; private set; }

    public double? Latitude => Location?.Y;
    public double? Longitude => Location?.X;

    public static Property Create(
        Guid organizationId,
        Address address,
        string county,
        string? parcelNumber,
        Guid createdBy,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            organizationId,
            address,
            county,
            parcelNumber,
            createdBy,
            createdAt);

    public static Property CreateProfile(
        Guid organizationId,
        Address address,
        string county,
        string? parcelNumber,
        string? propertyType,
        int? yearBuilt,
        int? squareFeet,
        string? roofType,
        int? roofInstallationYear,
        string? ownerName,
        string? occupancyType,
        double? latitude,
        double? longitude,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        var property = new Property(
            Guid.NewGuid(),
            organizationId,
            address,
            county,
            parcelNumber,
            createdBy,
            createdAt);
        property.SetProfile(
            propertyType,
            yearBuilt,
            squareFeet,
            roofType,
            roofInstallationYear,
            ownerName,
            occupancyType,
            latitude,
            longitude,
            createdAt.Year);
        return property;
    }

    public void UpdateProfile(
        Address address,
        string county,
        string? parcelNumber,
        string? propertyType,
        int? yearBuilt,
        int? squareFeet,
        string? roofType,
        int? roofInstallationYear,
        string? ownerName,
        string? occupancyType,
        double? latitude,
        double? longitude,
        Guid modifiedBy,
        DateTimeOffset modifiedAt)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted property cannot be updated.");
        }

        Address = address ?? throw new ArgumentNullException(nameof(address));
        AddressKey = address.ToIdentityKey();
        County = NormalizeRequired(county, "county", 100);
        ParcelNumber = NormalizeOptional(parcelNumber, 100, upperCase: true);
        SetProfile(
            propertyType,
            yearBuilt,
            squareFeet,
            roofType,
            roofInstallationYear,
            ownerName,
            occupancyType,
            latitude,
            longitude,
            modifiedAt.Year);
        ModifiedBy = RequireId(modifiedBy, "actor");
        ModifiedAt = modifiedAt;
        Version++;
    }

    private void SetProfile(
        string? propertyType,
        int? yearBuilt,
        int? squareFeet,
        string? roofType,
        int? roofInstallationYear,
        string? ownerName,
        string? occupancyType,
        double? latitude,
        double? longitude,
        int currentYear)
    {
        ValidateYear(yearBuilt, nameof(yearBuilt), currentYear);
        ValidateYear(roofInstallationYear, nameof(roofInstallationYear), currentYear);
        if (yearBuilt.HasValue &&
            roofInstallationYear.HasValue &&
            roofInstallationYear.Value < yearBuilt.Value)
        {
            throw new ArgumentException(
                "The roof installation year cannot precede the property construction year.",
                nameof(roofInstallationYear));
        }

        if (squareFeet is <= 0 or > 100_000_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(squareFeet),
                "Square footage must be between 1 and 100,000,000.");
        }

        PropertyType = NormalizeOptional(propertyType, 100);
        YearBuilt = yearBuilt;
        SquareFeet = squareFeet;
        RoofType = NormalizeOptional(roofType, 100);
        RoofInstallationYear = roofInstallationYear;
        OwnerName = NormalizeOptional(ownerName, 200);
        OccupancyType = NormalizeOptional(occupancyType, 100);
        Location = CreateLocation(latitude, longitude);
    }

    private static Point? CreateLocation(double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException("Latitude and longitude must be provided together.", nameof(latitude));
        }

        if (!latitude.HasValue)
        {
            return null;
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                "Longitude must be between -180 and 180.");
        }

        var validatedLatitude = latitude.GetValueOrDefault();
        var validatedLongitude = longitude.GetValueOrDefault();
        return new Point(validatedLongitude, validatedLatitude) { SRID = 4326 };
    }

    private static void ValidateYear(int? value, string name, int currentYear)
    {
        if (value is < 1600 || value > currentYear)
        {
            throw new ArgumentOutOfRangeException(
                name,
                $"The year must be between 1600 and {currentYear}.");
        }
    }

    private static Guid RequireId(Guid value, string name) =>
        value != Guid.Empty
            ? value
            : throw new ArgumentException($"A {name} ID is required.", name);

    private static string NormalizeRequired(string value, string name, int maximumLength) =>
        NormalizeOptional(value, maximumLength)
        ?? throw new ArgumentException($"A property {name} is required.", name);

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        bool upperCase = false)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                nameof(value));
        }

        return upperCase ? normalized.ToUpperInvariant() : normalized;
    }
}
