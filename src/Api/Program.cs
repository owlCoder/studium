using System.Security.Claims;
using Api.Identity;
using Application.Courses;
using Application.Engagements;
using Application.Enrollments;
using Application.Identity;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
// US-7.1: izvođenja na koja je student upisan
builder.Services.AddSingleton<IStudentOfferingsReader>(_ => DemoData.CreateStudentOfferingsReader());
builder.Services.AddScoped<ListStudentOfferingsHandler>();

// US-7.2: izvođenja na kojima je korisnik angažovan, uz listu upisa iz US-7.1
builder.Services.AddSingleton<IStaffOfferingsReader>(_ => DemoData.CreateStaffOfferingsReader());
builder.Services.AddScoped<ListMyOfferingsHandler>();

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

// US-7.5: vraća ID prijavljenog korisnika, uzet isključivo iz sesije.
app.MapGet("/api/ja", (ClaimsPrincipal user) =>
        user.TryGetSessionUserId(out var userId)
            ? Results.Ok(new { userId })
            : Results.Unauthorized())
    .RequireAuthorization();

app.Run();

// US-7.5: omogućava HTTP testove preko WebApplicationFactory<Program>.
public partial class Program { }
