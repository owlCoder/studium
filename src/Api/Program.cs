using Application.Auth;
using Application.Courses;
using Application.Time;
using Application.Users;
using Infrastructure.Demo;
using Infrastructure.Security;
using Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// ST-002 osnova: heširanje lozinki i jedinstven izvor vremena
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IClock, SystemClock>();

// ST-002 Prijava: handler zavisi od portova, a ovde se biraju konkretne implementacije.
builder.Services.AddSingleton<IUserLookup>(
    sp => DemoData.CreateUserLookup(sp.GetRequiredService<IPasswordHasher>()));
builder.Services.AddScoped<LoginHandler>();

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

// ST-002 Prijava. Endpoint je tanak: prevodi HTTP u komandu i ishod u status.
// Poslovna pravila su u LoginHandler-u.
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    LoginHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new LoginCommand(request.Username, request.Password), cancellationToken);

    if (result.Succeeded)
    {
        return Results.Ok(result.Identity);
    }

    return result.Failure switch
    {
        LoginFailure.InvalidInput =>
            Results.BadRequest(new { error = "Korisničko ime i lozinka su obavezni." }),
        // Ista poruka za nepoznato ime, pogrešnu lozinku i neaktivan nalog (ST-002).
        _ => Results.Json(new { error = "Neuspešna prijava." },
            statusCode: StatusCodes.Status401Unauthorized)
    };
});

app.Run();

/// <summary>HTTP ulaz prijave.</summary>
public sealed record LoginRequest(string? Username, string? Password);
