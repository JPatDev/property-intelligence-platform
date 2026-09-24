using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Identity;

namespace PropertyIntelligence.Modules.Properties.UnitTests;

public sealed class PropertySecurityBoundaryTests
{
    [Fact]
    public async Task Property_mutation_requires_tenant_identity_and_allowed_role()
    {
        await using var services = CreateServices();
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(userId, organizationId, PlatformRoles.PublicAdjuster),
            null,
            PlatformPolicies.ManageProperties)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(userId, organizationId, PlatformRoles.ReadOnly),
            null,
            PlatformPolicies.ManageProperties)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "test")),
            null,
            PlatformPolicies.ManageProperties)).Succeeded);
    }

    private static ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "DevelopmentHeaders",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        new IdentityModule().AddServices(services, configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static ClaimsPrincipal Principal(Guid userId, Guid organizationId, string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(PlatformClaimTypes.OrganizationId, organizationId.ToString()),
                new Claim(ClaimTypes.Role, role),
            ],
            "test"));
}
