using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Identity;

namespace PropertyIntelligence.Modules.Workflow.UnitTests;

public sealed class SecurityBoundaryTests
{
    [Fact]
    public async Task Workflow_mutation_requires_tenant_identity_and_allowed_role()
    {
        await using var services = CreateServices();
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var allowed = Principal(userId, organizationId, PlatformRoles.PublicAdjuster);
        var readOnly = Principal(userId, organizationId, PlatformRoles.ReadOnly);
        var missingTenant = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test"));

        Assert.True((await authorization.AuthorizeAsync(
            allowed,
            null,
            PlatformPolicies.ManageWorkflow)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            readOnly,
            null,
            PlatformPolicies.ManageWorkflow)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            missingTenant,
            null,
            PlatformPolicies.ManageWorkflow)).Succeeded);
    }

    [Fact]
    public async Task Request_identity_is_derived_from_authenticated_claims()
    {
        await using var services = CreateServices();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        services.GetRequiredService<IHttpContextAccessor>().HttpContext =
            new DefaultHttpContext
            {
                User = Principal(userId, organizationId, PlatformRoles.Manager),
            };
        await using var scope = services.CreateAsyncScope();

        var identity = scope.ServiceProvider.GetRequiredService<IRequestIdentity>();

        Assert.Equal(userId, identity.UserId);
        Assert.Equal(organizationId, identity.OrganizationId);
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

    private static ClaimsPrincipal Principal(
        Guid userId,
        Guid organizationId,
        string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(PlatformClaimTypes.OrganizationId, organizationId.ToString()),
                new Claim(ClaimTypes.Role, role),
            ],
            "test"));
}
