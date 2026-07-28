using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PropertyIntelligence.Api.OpenApi;

internal static class DevelopmentHeaderSecurity
{
    public const string UserId = "DevelopmentUserId";
    public const string OrganizationId = "DevelopmentOrganizationId";
    public const string Roles = "DevelopmentRoles";
}

internal sealed class DevelopmentHeaderSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() ||
            !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        var responses = operation.Responses ??= new OpenApiResponses();
        responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse { Description = "Authentication is required." });
        responses.TryAdd(
            StatusCodes.Status403Forbidden.ToString(),
            new OpenApiResponse { Description = "The authenticated identity is not authorized." });
    }
}
