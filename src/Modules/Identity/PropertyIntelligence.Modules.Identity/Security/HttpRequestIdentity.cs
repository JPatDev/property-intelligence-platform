using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PropertyIntelligence.BuildingBlocks.Security;

namespace PropertyIntelligence.Modules.Identity.Security;

internal sealed class HttpRequestIdentity(IHttpContextAccessor httpContextAccessor)
    : IRequestIdentity
{
    public Guid UserId => ReadRequiredGuid(
        ClaimTypes.NameIdentifier,
        PlatformClaimTypes.ObjectId,
        "sub");

    public Guid OrganizationId => ReadRequiredGuid(PlatformClaimTypes.OrganizationId);

    private Guid ReadRequiredGuid(params string[] claimTypes)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        var value = claimTypes
            .Select(claimType => principal?.FindFirstValue(claimType))
            .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException(
                $"The authenticated principal is missing a valid '{claimTypes[0]}' claim.");
    }
}
