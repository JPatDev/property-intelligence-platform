using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Security;
using PropertyIntelligence.Modules.Documents.Domain;
using PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Documents.Api;

internal static class DocumentEndpoints
{
    public static RouteGroupBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization(PlatformPolicies.TenantAccess);

        group.MapPost(
                "/",
                async (
                    RegisterDocumentRequest request,
                    IRequestIdentity identity,
                    DocumentsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var document = DocumentRecord.Register(
                            identity.OrganizationId,
                            request.ClaimId,
                            request.DocumentType,
                            request.FileName,
                            request.StoragePath,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        dbContext.Documents.Add(document);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Created(
                            $"/api/documents/{document.Id}",
                            ToResponse(document));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                })
            .WithName("RegisterDocument")
            .RequireAuthorization(PlatformPolicies.ManageDocuments)
            .Produces<DocumentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet(
                "/{documentId:guid}",
                async (
                    Guid documentId,
                    IRequestIdentity identity,
                    DocumentsDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var document = await dbContext.Documents
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            candidate =>
                                candidate.OrganizationId == identity.OrganizationId &&
                                candidate.Id == documentId,
                            cancellationToken);
                    return document is null
                        ? Results.NotFound()
                        : Results.Ok(ToResponse(document));
                })
            .WithName("GetDocument")
            .Produces<DocumentResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut(
                "/{documentId:guid}/status",
                async (
                    Guid documentId,
                    ChangeDocumentStatusRequest request,
                    IRequestIdentity identity,
                    DocumentsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var document = await dbContext.Documents.SingleOrDefaultAsync(
                        candidate =>
                            candidate.OrganizationId == identity.OrganizationId &&
                            candidate.Id == documentId,
                        cancellationToken);
                    if (document is null)
                    {
                        return Results.NotFound();
                    }

                    if (document.Version != request.ExpectedVersion)
                    {
                        return Conflict();
                    }

                    try
                    {
                        document.ChangeStatus(
                            request.Status,
                            identity.UserId,
                            timeProvider.GetUtcNow());
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Ok(ToResponse(document));
                    }
                    catch (ArgumentException exception)
                    {
                        return Validation(exception);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Conflict(new
                        {
                            code = "document.transition_invalid",
                            detail = exception.Message,
                        });
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return Conflict();
                    }
                })
            .WithName("ChangeDocumentStatus")
            .RequireAuthorization(PlatformPolicies.ManageDocuments)
            .Produces<DocumentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static DocumentResponse ToResponse(DocumentRecord document) =>
        new(
            document.Id,
            document.OrganizationId,
            document.ClaimId,
            document.DocumentType,
            document.FileName,
            document.StoragePath,
            document.Status,
            document.Version,
            document.CreatedAt,
            document.ModifiedAt);

    private static IResult Validation(ArgumentException exception) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "request"] = [exception.Message],
        });

    private static IResult Conflict() =>
        Results.Conflict(new
        {
            code = "document.concurrency_conflict",
            detail = "The document was changed by another operation. Reload it and retry.",
        });
}

public sealed record RegisterDocumentRequest(
    Guid ClaimId,
    string DocumentType,
    string FileName,
    string StoragePath);

public sealed record ChangeDocumentStatusRequest(
    DocumentStatus Status,
    long ExpectedVersion);

public sealed record DocumentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid ClaimId,
    string DocumentType,
    string FileName,
    string StoragePath,
    DocumentStatus Status,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);
