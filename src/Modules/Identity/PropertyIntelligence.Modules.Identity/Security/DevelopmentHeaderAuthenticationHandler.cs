using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyIntelligence.BuildingBlocks.Security;

namespace PropertyIntelligence.Modules.Identity.Security;

internal sealed class DevelopmentHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentHeaders";
    private const string UserHeader = "X-User-Id";
    private const string OrganizationHeader = "X-Organization-Id";
    private const string RolesHeader = "X-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers[UserHeader].FirstOrDefault(), out var userId) ||
            !Guid.TryParse(Request.Headers[OrganizationHeader].FirstOrDefault(), out var organizationId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(PlatformClaimTypes.ObjectId, userId.ToString()),
            new(PlatformClaimTypes.OrganizationId, organizationId.ToString()),
        };
        var roles = Request.Headers[RolesHeader]
            .SelectMany(value => value?.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
