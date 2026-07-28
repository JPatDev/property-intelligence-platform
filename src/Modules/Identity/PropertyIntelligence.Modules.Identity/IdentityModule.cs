using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Identity.Security;

namespace PropertyIntelligence.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var authenticationMode = configuration["Authentication:Mode"] ?? "JwtBearer";
        if (authenticationMode is not ("JwtBearer" or DevelopmentHeaderAuthenticationHandler.SchemeName))
        {
            throw new InvalidOperationException(
                $"Authentication mode '{authenticationMode}' is not supported.");
        }

        var authenticationScheme = string.Equals(
            authenticationMode,
            DevelopmentHeaderAuthenticationHandler.SchemeName,
            StringComparison.Ordinal)
            ? DevelopmentHeaderAuthenticationHandler.SchemeName
            : JwtBearerDefaults.AuthenticationScheme;
        var authentication = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = authenticationScheme;
            options.DefaultChallengeScheme = authenticationScheme;
        });

        if (string.Equals(
                authenticationMode,
                DevelopmentHeaderAuthenticationHandler.SchemeName,
                StringComparison.Ordinal))
        {
            authentication.AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(
                DevelopmentHeaderAuthenticationHandler.SchemeName,
                _ => { });
        }
        else
        {
            var authority = configuration["Authentication:Authority"];
            var audience = configuration["Authentication:Audience"];
            if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "JWT authentication requires Authentication:Authority and Authentication:Audience.");
            }

            authentication.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata =
                    configuration.GetValue("Authentication:RequireHttpsMetadata", true);
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
            });
        }

        services.AddHttpContextAccessor();
        services.AddScoped<IRequestIdentity, HttpRequestIdentity>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                PlatformPolicies.TenantAccess,
                policy => RequireTenantIdentity(policy));
            AddRolePolicy(
                options,
                PlatformPolicies.ManageClaims,
                PlatformRoles.Owner,
                PlatformRoles.Administrator,
                PlatformRoles.Manager,
                PlatformRoles.PublicAdjuster,
                PlatformRoles.Assistant);
            AddRolePolicy(
                options,
                PlatformPolicies.ManageDocuments,
                PlatformRoles.Owner,
                PlatformRoles.Administrator,
                PlatformRoles.Manager,
                PlatformRoles.PublicAdjuster,
                PlatformRoles.Assistant);
            AddRolePolicy(
                options,
                PlatformPolicies.ManageCommunications,
                PlatformRoles.Owner,
                PlatformRoles.Administrator,
                PlatformRoles.Manager,
                PlatformRoles.PublicAdjuster,
                PlatformRoles.Assistant);
            AddRolePolicy(
                options,
                PlatformPolicies.ManageWorkflow,
                PlatformRoles.Owner,
                PlatformRoles.Administrator,
                PlatformRoles.Manager,
                PlatformRoles.PublicAdjuster,
                PlatformRoles.Assistant);
            AddRolePolicy(
                options,
                PlatformPolicies.ManagePlaybooks,
                PlatformRoles.Owner,
                PlatformRoles.Administrator,
                PlatformRoles.Manager);
            AddRolePolicy(
                options,
                PlatformPolicies.ViewWorkflowAudit,
                PlatformRoles.Owner,
                PlatformRoles.Administrator,
                PlatformRoles.Manager);
        });
    }

    private static void AddRolePolicy(
        AuthorizationOptions options,
        string name,
        params string[] roles) =>
        options.AddPolicy(name, policy =>
        {
            RequireTenantIdentity(policy);
            policy.RequireRole(roles);
        });

    private static void RequireTenantIdentity(AuthorizationPolicyBuilder policy)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(PlatformClaimTypes.OrganizationId);
        policy.RequireAssertion(context =>
            context.User.HasClaim(claim =>
                claim.Type is ClaimTypes.NameIdentifier or PlatformClaimTypes.ObjectId or "sub"));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "identity", status = "ready" }));
    }
}
