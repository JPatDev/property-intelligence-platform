namespace PropertyIntelligence.Modules.Claims.Domain;

public sealed class Claim
{
    private Claim()
    {
    }

    private Claim(
        Guid id,
        Guid organizationId,
        Guid propertyId,
        string claimNumber,
        string? policyNumber,
        DateOnly? dateOfLoss,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = organizationId;
        PropertyId = propertyId;
        ClaimNumber = NormalizeRequired(claimNumber, "A claim number is required.", 100);
        PolicyNumber = NormalizeOptional(policyNumber, 100);
        DateOfLoss = ValidateDateOfLoss(dateOfLoss, createdAt);
        Status = ClaimStatus.Intake;
        CreatedBy = RequireActor(createdBy);
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string ClaimNumber { get; private set; } = string.Empty;
    public string? PolicyNumber { get; private set; }
    public DateOnly? DateOfLoss { get; private set; }
    public ClaimStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public long Version { get; private set; }

    public static Claim Create(
        Guid organizationId,
        Guid propertyId,
        string claimNumber,
        string? policyNumber,
        DateOnly? dateOfLoss,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization ID is required.", nameof(organizationId));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("A property ID is required.", nameof(propertyId));
        }

        return new Claim(
            Guid.NewGuid(),
            organizationId,
            propertyId,
            claimNumber,
            policyNumber,
            dateOfLoss,
            createdBy,
            createdAt);
    }

    public void UpdateFacts(
        string? policyNumber,
        DateOnly? dateOfLoss,
        Guid modifiedBy,
        DateTimeOffset modifiedAt)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted claim cannot be updated.");
        }

        PolicyNumber = NormalizeOptional(policyNumber, 100);
        DateOfLoss = ValidateDateOfLoss(dateOfLoss, modifiedAt);
        ModifiedBy = RequireActor(modifiedBy);
        ModifiedAt = modifiedAt;
        Version++;
    }

    private static Guid RequireActor(Guid actorId) =>
        actorId != Guid.Empty
            ? actorId
            : throw new ArgumentException("An actor ID is required.", nameof(actorId));

    private static string NormalizeRequired(string value, string message, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException(message, nameof(value));
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", nameof(value));
        }

        return normalized;
    }

    private static DateOnly? ValidateDateOfLoss(DateOnly? dateOfLoss, DateTimeOffset changedAt)
    {
        if (dateOfLoss > DateOnly.FromDateTime(changedAt.UtcDateTime))
        {
            throw new ArgumentException("The date of loss cannot be in the future.", nameof(dateOfLoss));
        }

        return dateOfLoss;
    }
}
