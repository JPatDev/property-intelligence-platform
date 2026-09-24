using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Properties.Contracts;
using PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Properties.Infrastructure;

internal sealed class PropertyDirectory(PropertiesDbContext dbContext) : IPropertyDirectory
{
    public Task<PropertyReference?> FindAsync(
        Guid organizationId,
        Guid propertyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Properties
            .AsNoTracking()
            .Where(property =>
                property.OrganizationId == organizationId &&
                property.Id == propertyId)
            .Select(property => new PropertyReference(
                property.Id,
                property.Address.Street,
                property.Address.City,
                property.Address.State,
                property.Address.PostalCode,
                property.County,
                property.ParcelNumber))
            .SingleOrDefaultAsync(cancellationToken);
}
