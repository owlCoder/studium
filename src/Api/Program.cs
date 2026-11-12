using System.Security.Claims;
using Application.Courses;
using Application.Engagements;
using Application.Enrollments;
using Infrastructure.Demo;

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

// US-7.1: identitet se uzima samo iz sesije (claim NameIdentifier), nikad iz zahtjeva.
// Dok US-7.5 ne uvede autentifikaciju, endpoint uvek vraća 401. Dijana
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
});

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
});

app.Run();
