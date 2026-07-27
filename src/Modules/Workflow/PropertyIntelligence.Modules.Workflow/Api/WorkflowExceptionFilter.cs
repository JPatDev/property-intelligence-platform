using Microsoft.AspNetCore.Http;
using PropertyIntelligence.Modules.Workflow.Application.Errors;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Api;

internal sealed class WorkflowExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (WorkflowValidationException exception)
        {
            return Results.ValidationProblem(
                exception.Errors,
                title: "Workflow request validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: CodeExtension(exception.Code));
        }
        catch (WorkflowNotFoundException exception)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "Workflow not found",
                exception);
        }
        catch (WorkflowEscalationNotFoundException exception)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "Workflow escalation not found",
                exception);
        }
        catch (WorkflowDefinitionNotFoundException exception)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "Workflow definition not found",
                exception);
        }
        catch (WorkflowDefinitionTypeMismatchException exception)
        {
            return Problem(
                StatusCodes.Status409Conflict,
                "Workflow definition type mismatch",
                exception);
        }
        catch (WorkflowConcurrencyException exception)
        {
            return Problem(
                StatusCodes.Status409Conflict,
                "Workflow concurrency conflict",
                exception);
        }
        catch (WorkflowDomainException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Workflow operation rejected",
                detail: exception.Message,
                extensions: CodeExtension(exception.Code));
        }
    }

    private static IResult Problem(
        int statusCode,
        string title,
        WorkflowApplicationException exception) =>
        Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: exception.Message,
            extensions: CodeExtension(exception.Code));

    private static Dictionary<string, object?> CodeExtension(string code) =>
        new(StringComparer.Ordinal)
        {
            ["code"] = code,
        };
}
