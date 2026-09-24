namespace PropertyIntelligence.Modules.Properties.Contracts;

public interface IPropertyDirectory
{
    Task<PropertyReference?> FindAsync(
        Guid organizationId,
        Guid propertyId,
        CancellationToken cancellationToken = default);
}

public sealed record PropertyReference(
    Guid Id,
    string Street,
    string City,
    string State,
    string PostalCode,
    string County,
    string? ParcelNumber);
