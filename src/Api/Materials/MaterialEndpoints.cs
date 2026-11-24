using System.Security.Claims;
using Application.Materials;

namespace Api.Materials;

/// <summary>
/// HTTP endpointi ST-009 (nastavni materijali). Poslovna pravila su u slučajevima upotrebe
/// (<c>Application.Materials</c>); endpointi samo čitaju identitet, pozivaju slučaj upotrebe
/// i prevode ishod u HTTP status.
/// Zajednički redosled: 401 bez identiteta → 404 ako materijal ne postoji → 403 ako korisnik
/// nije nastavno osoblje izvođenja (ST-005) → 400 za neispravne podatke → uspeh.
/// Identitet se čita iz <see cref="ClaimsPrincipal"/> (<c>NameIdentifier</c>).
/// </summary>
public static class MaterialEndpoints
{
    public static IEndpointRouteBuilder MapMaterialEndpoints(this IEndpointRouteBuilder app)
    {
        // US-01: kreiranje materijala kao nacrta na konkretnom izvođenju.
        app.MapPost("/offerings/{offeringId:guid}/materials", CreateMaterialAsync);

        // US-10: lista objavljenih materijala izvođenja za upisanog studenta (ST-006).
        app.MapGet("/offerings/{offeringId:guid}/materials", ListPublishedMaterialsForStudentAsync);

        // US-04/05/06: otpremanje jedne ili više datoteka (multipart/form-data).
        // Antiforgery je isključen jer način prijave (ST-002) još nije izabran;
        // uz prijavu kolačićem zaštitu treba uključiti.
        app.MapPost("/materials/{materialId:guid}/attachments", UploadAttachmentsAsync)
            .DisableAntiforgery();

        // US-07: uklanjanje jednog priloga (zapis, pa datoteka iz skladišta).
        app.MapDelete("/materials/{materialId:guid}/attachments/{attachmentId:guid}", RemoveAttachmentAsync);

        // US-07: uklanjanje jedne spoljne veze.
        app.MapDelete("/materials/{materialId:guid}/links/{linkId:guid}", RemoveExternalLinkAsync);

        // Zaštita pristupa, tačka 1: preuzimanje jednog priloga uz proveru članstva pri svakom zahtevu.
        app.MapGet("/materials/{materialId:guid}/attachments/{attachmentId:guid}", DownloadAttachmentAsync);

        return app;
    }

    private static async Task<IResult> CreateMaterialAsync(
        Guid offeringId,
        CreateMaterialRequest request,
        HttpContext httpContext,
        CreateMaterialHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
            return Results.Unauthorized();

        return await ExecuteAsync(validationKey: null, async () =>
        {
            var id = await handler.HandleAsync(
                new CreateMaterialCommand(userId, offeringId, request.Name ?? string.Empty, request.Description),
                cancellationToken);

            return Results.Created(
                $"/offerings/{offeringId}/materials/{id}",
                new CreateMaterialResponse(id, "Draft"));
        });
    }

    // US-10: 401 bez identiteta → 403 ako korisnik nije aktivan student izvođenja (ST-006)
    // → 200 sa listom objavljenih materijala (naziv i opis); prazna lista ako ih nema.
    // Nacrti i povučeni materijali se ne vraćaju.
    private static async Task<IResult> ListPublishedMaterialsForStudentAsync(
        Guid offeringId,
        HttpContext httpContext,
        ListPublishedMaterialsForStudentHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
            return Results.Unauthorized();

        return await ExecuteAsync(validationKey: null, async () =>
        {
            var materials = await handler.HandleAsync(
                new ListPublishedMaterialsForStudentQuery(userId, offeringId),
                cancellationToken);

            return Results.Ok(materials);
        });
    }

    // 201 ako su sve datoteke sačuvane, 200 ako bar jedna nije; telo uvek sadrži
    // rezultat po datoteci (US-06). Neispravna ili odbijena datoteka (US-05) → 400, ništa se ne čuva.
    private static async Task<IResult> UploadAttachmentsAsync(
        Guid materialId,
        IFormFileCollection files,
        HttpContext httpContext,
        UploadMaterialAttachmentsHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
            return Results.Unauthorized();

        var uploaded = files
            .Select(f => new UploadedFile(f.FileName, f.ContentType, f.Length, f.OpenReadStream))
            .ToArray();

        return await ExecuteAsync(validationKey: "files", async () =>
        {
            var results = await handler.HandleAsync(
                new UploadMaterialAttachmentsCommand(userId, materialId, uploaded),
                cancellationToken);

            if (results is null)
                return Results.NotFound();

            return results.All(r => r.Saved)
                ? Results.Created((string?)null, results)
                : Results.Ok(results);
        });
    }

    // 204 kada je prilog uklonjen; 404 ako materijal ili prilog ne postoji ili prilog pripada drugom materijalu.
    private static async Task<IResult> RemoveAttachmentAsync(
        Guid materialId,
        Guid attachmentId,
        HttpContext httpContext,
        RemoveMaterialAttachmentHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
            return Results.Unauthorized();

        return await ExecuteAsync(validationKey: null, async () =>
        {
            var outcome = await handler.HandleAsync(
                new RemoveMaterialAttachmentCommand(userId, materialId, attachmentId),
                cancellationToken);

            return outcome == RemoveMaterialAttachmentOutcome.Removed
                ? Results.NoContent()
                : Results.NotFound();
        });
    }

    // 204 kada je veza uklonjena; 404 ako materijal ili veza ne postoji ili veza pripada drugom materijalu.
    private static async Task<IResult> RemoveExternalLinkAsync(
        Guid materialId,
        Guid linkId,
        HttpContext httpContext,
        RemoveMaterialExternalLinkHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
            return Results.Unauthorized();

        return await ExecuteAsync(validationKey: null, async () =>
        {
            var outcome = await handler.HandleAsync(
                new RemoveMaterialExternalLinkCommand(userId, materialId, linkId),
                cancellationToken);

            return outcome == RemoveMaterialExternalLinkOutcome.Removed
                ? Results.NoContent()
                : Results.NotFound();
        });
    }

    // Prilog nasleđuje pravo materijala: preuzima ga samo nastavno osoblje (ST-005) ili aktivan
    // student (ST-006) izvođenja materijala. 403 bez prava; 404 ako materijal ili prilog ne postoji,
    // prilog pripada drugom materijalu ili mu nedostaje sadržaj; 200 sa datotekom.
    private static async Task<IResult> DownloadAttachmentAsync(
        Guid materialId,
        Guid attachmentId,
        HttpContext httpContext,
        DownloadMaterialAttachmentHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
            return Results.Unauthorized();

        return await ExecuteAsync(validationKey: null, async () =>
        {
            var result = await handler.HandleAsync(
                new DownloadMaterialAttachmentCommand(userId, materialId, attachmentId),
                cancellationToken);

            if (result.Outcome != DownloadMaterialAttachmentOutcome.Found)
                return Results.NotFound();

            // Results.File zatvara tok po slanju odgovora.
            return Results.File(
                result.Content!,
                result.Attachment!.ContentType,
                result.Attachment.OriginalFileName);
        });
    }

    /// <summary>
    /// Identitet prijavljenog korisnika. Dok ST-002 ne doda pravu prijavu, identitet daje
    /// demo prijava tima ST-006 (samo Development/KT1); bez nje svaki zahtev dobija 401.
    /// </summary>
    private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId) && userId != Guid.Empty;
    }

    /// <summary>
    /// Prevodi izuzetke slučajeva upotrebe ST-009 u HTTP odgovore:
    /// <see cref="MaterialAccessDeniedException"/> → 403,
    /// <see cref="AttachmentRejectedException"/> i <see cref="ArgumentException"/> → 400.
    /// <paramref name="validationKey"/> je ključ greške u odgovoru; <c>null</c> znači naziv
    /// parametra iz izuzetka (npr. <c>name</c>).
    /// </summary>
    private static async Task<IResult> ExecuteAsync(string? validationKey, Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (MaterialAccessDeniedException ex)
        {
            // Results.Forbid() zahteva šemu prijave koja podržava zabranu; ovde se vraća 403 sa porukom.
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (AttachmentRejectedException ex)
        {
            return ValidationProblem(validationKey ?? "files", ex.Message);
        }
        catch (ArgumentException ex)
        {
            var message = ex.ParamName is null
                ? ex.Message
                : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty);

            return ValidationProblem(validationKey ?? ex.ParamName ?? "request", message);
        }
    }

    private static IResult ValidationProblem(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });
}

internal sealed record CreateMaterialRequest(string? Name, string? Description);

internal sealed record CreateMaterialResponse(Guid Id, string PublicationState);
