using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Documents.Application.Abstractions;
using FleetIQ.Documents.Application.CreateDocument;
using FleetIQ.Documents.Application.GetDocument;
using FleetIQ.Documents.Application.ListDocuments;
using FluentValidation;

namespace FleetIQ.Documents.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").WithTags("Document").RequireAuthorization();

        group.MapPost("/", async (
            CreateDocumentCommand command,
            IValidator<CreateDocumentCommand> validator,
            ICommandHandler<CreateDocumentCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/documents/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        // LIST recent - powers the Documents page in the SPA.
        group.MapGet("/", async (
            int? take,
            IQueryHandler<ListDocumentsQuery, IReadOnlyList<DocumentListItem>> handler,
            CancellationToken ct) =>
        {
            return Results.Ok(await handler.Handle(new ListDocumentsQuery(take ?? 20), ct));
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetDocumentQuery, DocumentResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetDocumentQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Verify -> Verified
        group.MapPost("/{id:guid}/verify", async (
            Guid id,
            IDocumentRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Verify();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // State transition: Reject (with a reason)
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectDocumentRequest request,
            IDocumentRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Reject(request.Reason);
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // Upload a replacement file: returns the document to Uploaded for re-verification.
        group.MapPost("/{id:guid}/versions", async (
            Guid id,
            AddDocumentVersionRequest request,
            IDocumentRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.AddVersion(request.StorageRef, request.ExpiresOnUtc);
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // State transition: Archive (read-only afterwards)
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            IDocumentRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Archive();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}

public sealed record RejectDocumentRequest(string Reason);
public sealed record AddDocumentVersionRequest(string StorageRef, DateTime? ExpiresOnUtc = null);
