using System.Security.Claims;
using Application.Courses;
using Application.Materials;
using Infrastructure.Demo;
using Infrastructure.Materials;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// ST-009: memorijsko skladište materijala do odluke o bazi (ADR).
builder.Services.AddSingleton<IMaterialRepository, InMemoryMaterialRepository>();
builder.Services.AddScoped<CreateMaterialHandler>();
// ST-005 još nije integrisan: privremeni adapter bez angažovanja odbija sve (403).
builder.Services.AddSingleton<IOfferingStaffChecker>(_ => new InMemoryOfferingStaffChecker());

// ST-009 (US-05): dozvoljeni tipovi i maksimalna veličina priloga iz konfiguracije.
// Nedostajuća ili neispravna konfiguracija zaustavlja pokretanje umesto da prihvati sve.
var attachmentSection = builder.Configuration.GetSection("MaterialAttachments");
builder.Services.AddSingleton(new AttachmentUploadPolicy(
    attachmentSection.GetSection("AllowedExtensions").Get<string[]>() ?? [],
    attachmentSection.GetValue<long>("MaxSizeInBytes")));

// ST-009 (US-04): skladište sadržaja priloga na disku (ADR-0001) i podaci o prilozima.
// Relativna putanja se računa od korena aplikacije; bez konfiguracije pokretanje se zaustavlja.
var storageRootPath = attachmentSection.GetValue<string>("StorageRootPath");
if (string.IsNullOrWhiteSpace(storageRootPath))
{
    throw new InvalidOperationException(
        "Nije zadat 'MaterialAttachments:StorageRootPath' (direktorijum za priloge).");
}
builder.Services.AddSingleton<IAttachmentFileStorage>(new LocalDiskAttachmentFileStorage(
    Path.Combine(builder.Environment.ContentRootPath, storageRootPath)));
builder.Services.AddSingleton<IMaterialAttachmentRepository, InMemoryMaterialAttachmentRepository>();
builder.Services.AddScoped<AttachFileToMaterialHandler>();
builder.Services.AddScoped<UploadMaterialAttachmentsHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Javno dostupan demonstracioni endpoint, bez autentifikacije:
// ne predstavlja funkcionalnost ST-003 i ne koristiti ga za stvarne podatke.
app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.MapGet("/courses", async (
    string? query,
    SearchCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(courses);
});

// ST-009: kreiranje materijala kao nacrta na konkretnom izvođenju.
// Redosled: 401 bez identiteta -> 403 ako korisnik nije nastavno osoblje
// izvođenja (ST-005) -> 400 za neispravne podatke -> 201.
// Identitet se čita iz ClaimsPrincipal (NameIdentifier); dok ST-002 ne doda
// autentifikaciju, svaki zahtev dobija 401.
app.MapPost("/offerings/{offeringId:guid}/materials", async (
    Guid offeringId,
    CreateMaterialRequest request,
    HttpContext httpContext,
    CreateMaterialHandler handler,
    CancellationToken cancellationToken) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId) || userId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    try
    {
        var id = await handler.HandleAsync(
            new CreateMaterialCommand(userId, offeringId, request.Name ?? string.Empty, request.Description),
            cancellationToken);

        return Results.Created(
            $"/offerings/{offeringId}/materials/{id}",
            new CreateMaterialResponse(id, "Draft"));
    }
    catch (MaterialAccessDeniedException ex)
    {
        // Results.Forbid() zahteva registrovanu autentifikaciju koje još nema.
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
    }
    catch (ArgumentException ex)
    {
        var message = ex.ParamName is null
            ? ex.Message
            : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty);

        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [ex.ParamName ?? "request"] = [message]
        });
    }
});

// ST-009 (US-04): otpremanje jedne ili više datoteka u materijal (multipart/form-data).
// Redosled: 401 bez identiteta -> 404 ako materijal ne postoji -> 403 ako korisnik nije
// nastavno osoblje izvođenja materijala (ST-005) -> 400 za neispravne ili odbijene
// datoteke (US-05; tada se ne čuva nijedna) -> 201 sa listom sačuvanih priloga.
// Dok ST-002 ne doda autentifikaciju, svaki zahtev dobija 401.
// Antiforgery je isključen jer način autentifikacije (ST-002) još nije izabran;
// ako se izabere autentifikacija kolačićem, zaštitu treba uključiti.
app.MapPost("/materials/{materialId:guid}/attachments", async (
    Guid materialId,
    IFormFileCollection files,
    HttpContext httpContext,
    UploadMaterialAttachmentsHandler handler,
    CancellationToken cancellationToken) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId) || userId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    var uploaded = files
        .Select(f => new UploadedFile(f.FileName, f.ContentType, f.Length, f.OpenReadStream))
        .ToArray();

    try
    {
        var attachments = await handler.HandleAsync(
            new UploadMaterialAttachmentsCommand(userId, materialId, uploaded),
            cancellationToken);

        return attachments is null
            ? Results.NotFound()
            : Results.Created((string?)null, attachments);
    }
    catch (MaterialAccessDeniedException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
    }
    catch (AttachmentRejectedException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["files"] = [ex.Message]
        });
    }
    catch (ArgumentException ex)
    {
        var message = ex.ParamName is null
            ? ex.Message
            : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty);

        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["files"] = [message]
        });
    }
}).DisableAntiforgery();

app.Run();

internal sealed record CreateMaterialRequest(string? Name, string? Description);

internal sealed record CreateMaterialResponse(Guid Id, string PublicationState);
