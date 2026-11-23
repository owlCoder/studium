using System.Security.Claims;
using Api.Identity;
using Application.Courses;
using Application.Dashboard;
using Domain.Dashboard;
using Infrastructure.Dashboard;
using Application.Engagements;
using Application.Enrollments;
using Application.Identity;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Application.Activities;
using Application.Home;
using Infrastructure.Activities;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
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
builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

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

// US-7.1: identitet se uzima samo iz sesije (claim NameIdentifier), nikad iz zahtjeva.
// US-7.5: bez važeće sesije i aktivnog naloga zahtev se odbija sa 401.
app.MapGet("/api/student/izvodjenja", async (
    ClaimsPrincipal user,
    ListStudentOfferingsHandler handler,
    CancellationToken cancellationToken) =>
{
    var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(raw, out var studentId))
    {
        return Results.Unauthorized();
    }

    var offerings = await handler.HandleAsync(studentId, cancellationToken);
    return Results.Ok(offerings);
}).RequireAuthorization();

// US-7.2: obe liste prijavljenog korisnika („Predajem” i „Upisan sam”), bez duplikata.
app.MapGet("/api/moja-izvodjenja", async (
    ClaimsPrincipal user,
    ListMyOfferingsHandler handler,
    CancellationToken cancellationToken) =>
{
    var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(raw, out var userId))
    {
        return Results.Unauthorized();
    }

    var offerings = await handler.HandleAsync(userId, cancellationToken);
    return Results.Ok(offerings);
}).RequireAuthorization();

// US-7.4: ceo početni pregled u jednom JSON odgovoru.
// Identitet samo iz sesije (US-7.5), nikad iz zahteva; bez važeće sesije 401.
app.MapGet("/pocetna", async (
    ClaimsPrincipal user,
    HttpContext http,
    GetHomeOverviewHandler handler,
    CancellationToken cancellationToken) =>
{
    http.Response.Headers.CacheControl = "no-store";

    if (!user.TryGetSessionUserId(out var userId))
    {
        return Results.Unauthorized();
    }

    var overview = await handler.HandleAsync(userId, cancellationToken);
    return Results.Ok(overview);
}).RequireAuthorization();

// US-7.10: otvaranje veze ka izvođenju. Ovlašćenje se proverava pri svakom otvaranju,
// ne samo pri prikazu veze: tuđe izvođenje daje 404, bez otkrivanja da postoji.
app.MapGet("/offerings/{offeringId:guid}", async (
    Guid offeringId,
    ClaimsPrincipal user,
    CheckContentAccessHandler handler,
    CancellationToken cancellationToken) =>
{
    if (!user.TryGetSessionUserId(out var userId))
    {
        return Results.Unauthorized();
    }

    var offering = await handler.ForOfferingAsync(userId, offeringId, cancellationToken);
    return offering is null ? Results.NotFound() : Results.Ok(offering);
}).RequireAuthorization();

// US-7.10: otvaranje veze ka zadatku. Nacrt i zadatak tuđeg izvođenja daju 404 za studenta.
app.MapGet("/offerings/{offeringId:guid}/assignments/{activityId:guid}", async (
    Guid offeringId,
    Guid activityId,
    ClaimsPrincipal user,
    CheckContentAccessHandler handler,
    CancellationToken cancellationToken) =>
{
    if (!user.TryGetSessionUserId(out var userId))
    {
        return Results.Unauthorized();
    }

    var assignment = await handler.ForAssignmentAsync(userId, offeringId, activityId, cancellationToken);
    return assignment is null ? Results.NotFound() : Results.Ok(assignment);
}).RequireAuthorization();

// US-7.5: vraća ID prijavljenog korisnika, uzet isključivo iz sesije.
app.MapGet("/api/ja", (ClaimsPrincipal user) =>
        user.TryGetSessionUserId(out var userId)
            ? Results.Ok(new { userId })
            : Results.Unauthorized())
    .RequireAuthorization();

// US-7.6: lista podržanih vrsta aktivnosti i funkcionalnih oblasti
app.MapGet("/features", (FeatureRegistry registry) => Results.Ok(new
{
    activityTypes = registry.SupportedActivityTypes
        .Select(type => new { code = type.Code, area = type.Area.ToString() }),
    areas = registry.AvailableAreas.Select(area => area.ToString())
}));

// US-7.6: provera da li je vrsta aktivnosti podržana
app.MapGet("/features/activity-types/{activityType}", (string activityType, FeatureRegistry registry) =>
    Results.Ok(new { activityType, supported = registry.IsActivitySupported(activityType) }));

app.Run();

// US-7.5: omogućava HTTP testove preko WebApplicationFactory<Program>.
public partial class Program { }
