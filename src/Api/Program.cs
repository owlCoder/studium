using System.Security.Claims;
using Api.Home;
using Api.Identity;
using Api.Materials;
using Application.Courses;
using Application.Dashboard;
using Domain.Dashboard;
using Infrastructure.Dashboard;
using Application.Engagements;
using Application.Enrollments;
using Application.Identity;
using Api.Contracts;
using Application.Activities;
using Infrastructure.Activities;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Application.Home;
// Iz main (ST-006 i drugi timovi)
using Api.Authentication;
using Application.Interfaces;
using Application.Materials;
using Application.Services;
using Infrastructure.Materials;
using Infrastructure.Repositories;
var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// Iz main (ST-006): upisi i pristup izvođenju preko kontrolera.
builder.Services.AddControllers();
builder.Services.AddSingleton<IEnrollmentRepository>(
    _ => new EnrollmentRepository(DemoData.CreateEnrollments()));
builder.Services.AddSingleton<IOfferingAccessReader>(_ => DemoData.CreateOfferingAccessReader());
builder.Services.AddScoped<EnrollmentService>();

// US-7.6: registar podržanih funkcija (ST-007)
builder.Services.AddSingleton<FeatureRegistry>(_ => SupportedFeatures.CreateRegistry());
builder.Services.AddScoped<FilterSupportedActivitiesHandler>();
// US-7.1: izvođenja na koja je student upisan
builder.Services.AddSingleton<IStudentOfferingsReader>(_ => DemoData.CreateStudentOfferingsReader());
builder.Services.AddScoped<ListStudentOfferingsHandler>();

// US-7.2: izvođenja na kojima je korisnik angažovan, uz listu upisa iz US-7.1
builder.Services.AddSingleton<IStaffOfferingsReader>(_ => DemoData.CreateStaffOfferingsReader());
builder.Services.AddScoped<ListMyOfferingsHandler>();

// US-7.3: pravo pravilo otvorene aktivnosti 
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<InMemoryActivityStore>();
builder.Services.AddSingleton<IActivityReader>(sp => sp.GetRequiredService<InMemoryActivityStore>());
builder.Services.AddSingleton<IActivitySubmissionReader>(sp => sp.GetRequiredService<InMemoryActivityStore>());
// US-7.9: pojedinačni rokovi (ST-015); kad ST-015 uđe u main, ovde ide adapter nad njihovim IRokoviRepository.
builder.Services.AddSingleton<IIndividualDeadlineReader>(sp => sp.GetRequiredService<InMemoryActivityStore>());
builder.Services.AddSingleton<IOpenActivityCounter, OpenActivityCounter>();

// US-7.4: servis početnog pregleda (spaja US-7.1, US-7.2 i US-7.3; veze prema US-7.6)
builder.Services.AddScoped<GetHomeOverviewHandler>();

// US-7.10: veze ka svakom otvorenom zadatku i provera ovlašćenja pri otvaranju veze
builder.Services.AddSingleton<IOpenTaskLinkReader, OpenTaskLinkReader>();
builder.Services.AddScoped<CheckContentAccessHandler>();

// US-7.5: identitet iz sesije. Privremeni memorijski adapteri dok se ne integrišu ST-001 i ST-002;
// tada se samo ove dve registracije porta preusmere na njihove evidencije.
builder.Services.AddSingleton<InMemorySessionReader>();
builder.Services.AddSingleton<ISessionReader>(sp => sp.GetRequiredService<InMemorySessionReader>());
builder.Services.AddSingleton<InMemoryAccountStatusReader>();
builder.Services.AddSingleton<IAccountStatusReader>(sp => sp.GetRequiredService<InMemoryAccountStatusReader>());
builder.Services.AddScoped<ResolveSessionUserHandler>();

// Integracija ST-007 i main: u aplikaciji postoje dve prijave.
//  - StudiumSession (US-7.5): token sesije u zaglavlju "Authorization: Bearer ...";
//  - Demo (main, drugi tim): zaglavlje X-Demo-UserId, samo u Development/KT1.
// Podrazumevana šema samo prosleđuje zahtev: sa Bearer tokenom ide na sesiju, inače na Demo.
// Tako [Authorize] kontroleri drugog tima rade kao i pre, a ST-007 endpointi traže
// politiku "StudiumSessionOnly", pa za njih važi SAMO sesija (ID se nikad ne uzima iz zahteva).
const string DefaultScheme = "StudiumOrDemo";
const string SessionOnlyPolicy = HomeEndpoints.SessionOnlyPolicy;

builder.Services
    .AddAuthentication(DefaultScheme)
    .AddPolicyScheme(DefaultScheme, DefaultScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? SessionAuthenticationHandler.SchemeName
                : DemoAuthHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationHandler.SchemeName, _ => { })
    .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>(
        DemoAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SessionOnlyPolicy, policy => policy
        .AddAuthenticationSchemes(SessionAuthenticationHandler.SchemeName)
        .RequireAuthenticatedUser());
});

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
builder.Services.AddScoped<RemoveMaterialAttachmentHandler>();

// ST-009 (US-03): memorijsko skladište spoljnih veza materijala do odluke o bazi (ADR-0003).
// HTTP endpoint za dodavanje veze još nije dodat.
builder.Services.AddSingleton<IMaterialExternalLinkRepository, InMemoryMaterialExternalLinkRepository>();
builder.Services.AddScoped<AddMaterialExternalLinkHandler>();

// Memorijski adapter mora biti singleton da bi aktivnosti preživele između zahteva.
builder.Services.AddSingleton<IActivityRepository>(_ => DemoData.CreateActivityRepository());
builder.Services.AddSingleton<IActivityContextReader, InMemoryActivityContextReader>();
builder.Services.AddScoped<CreateActivityHandler>();
builder.Services.AddScoped<ListActivitiesHandler>();

var app = builder.Build();

// US-7.7: početna strana (wwwroot/index.html) i njeni statički fajlovi.
// Stranica ne sadrži podatke; kartice dobija tek preko zaštićenog GET /pocetna.
app.UseDefaultFiles();
app.UseStaticFiles();

// US-7.7: samo u Development okruženju pravi se sesija demo studenta,
// da se kartice mogu prikazati dok ST-002 ne uvede prijavu.
if (app.Environment.IsDevelopment())
{
    var demoToken = DemoSession.StartForDemoStudent(
        app.Services.GetRequiredService<InMemorySessionReader>(),
        app.Services.GetRequiredService<InMemoryAccountStatusReader>());
    app.Logger.LogInformation("Demo token sesije studenta (samo Development): {DemoToken}", demoToken);


    // US-7.8: demo nastavnik i asistentkinja, da se vidi sekcija „Predajem” sa oznakom uloge.
    var sessions = app.Services.GetRequiredService<InMemorySessionReader>();
    var accounts = app.Services.GetRequiredService<InMemoryAccountStatusReader>();
    app.Logger.LogInformation("Demo token sesije nastavnika (samo Development): {DemoToken}",
        DemoSession.StartForDemoTeacher(sessions, accounts));
    app.Logger.LogInformation("Demo token sesije asistentkinje (samo Development): {DemoToken}",
        DemoSession.StartForDemoAssistant(sessions, accounts));

    // US-7.10: demo zadaci na ERS (jedan objavljen, jedan nacrt), da se vide veze ka zadacima.
    DemoActivities.Seed(
        app.Services.GetRequiredService<InMemoryActivityStore>(),
        app.Services.GetRequiredService<TimeProvider>());

}


// US-7.5: svaki zahtev prolazi proveru sesije pre endpointa.
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Iz main (ST-006): kontroleri, npr. EnrollmentsController ([Authorize], prijava preko Demo šeme).
app.MapControllers();

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

// Demonstracioni tok aktivnosti: memorijski adapter i bez autentifikacije.
// Ne predstavlja završenu funkcionalnost ST-021 niti proveru prava nad izvođenjem.
// Oznaka konteksta je oblika OZNAKA-xxxxx, npr. ERS-11111; razrešava je
// IActivityContextReader, pa se kasnije može zameniti oznakom izvođenja predmeta.
app.MapPost("/Course/{contextSlug}/CreateActivity", async (
    string contextSlug,
    CreateActivityRequest request,
    CreateActivityHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new CreateActivityCommand(
            contextSlug,
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate),
        cancellationToken);

    return result.Status switch
    {
        CreateActivityStatus.Created => Results.Json(
            result.Activity,
            statusCode: StatusCodes.Status201Created),
        CreateActivityStatus.ContextNotFound => Results.Problem(
            title: "Predmet ili izvođenje nije pronađeno",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        CreateActivityStatus.InvalidRequest => Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["activity"] = new[] { result.Error ?? "Nevažeći podaci aktivnosti." }
            }),
        CreateActivityStatus.Conflict => Results.Problem(
            title: "Aktivnost već postoji",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        CreateActivityStatus.Canceled => Results.StatusCode(StatusCodes.Status499ClientClosedRequest),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/ListActivities", async (
    ListActivitiesHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(cancellationToken);

    return result.IsSuccess
        ? Results.Ok(result.Activities)
        : Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
});

// ST-007: endpointi početnog pregleda (US-7.1, 7.2, 7.4, 7.5, 7.6, 7.10) su u src/Api/Home/HomeEndpoints.cs.
app.MapHomeEndpoints();

// ST-009: endpointi za nastavne materijale i priloge (src/Api/Materials/MaterialEndpoints.cs).
app.MapMaterialEndpoints();

app.Run();

// US-7.5: omogućava HTTP testove preko WebApplicationFactory<Program>.
public partial class Program { }
