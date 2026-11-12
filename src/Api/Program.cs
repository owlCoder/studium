using Api;
using Application.Auth;
using Application.Courses;
using Application.Sessions;
using Application.Time;
using Application.Users;
using Infrastructure.Demo;
using Infrastructure.Security;
using Infrastructure.Sessions;
using Infrastructure.Time;
using Application.Access;
using Domain.Users;
using Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// ST-003 Aktivacija predmeta
builder.Services.AddSingleton<ICourseStatusRepository>(_ => DemoData.CreateCourseStatusRepository());
builder.Services.AddScoped<ActivateCourseHandler>();
builder.Services.AddScoped<AuthorizeRequestHandler>();

// ST-002 osnova: heširanje lozinki i jedinstven izvor vremena
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IClock, SystemClock>();

// ST-002 Prijava: handler zavisi od portova, a ovde se biraju konkretne implementacije.
// Demo korisnici se prave jednom (heširanje lozinki je namerno sporo), pa ih dele
// prijava (IUserLookup) i provera ovlašćenja (IAccountReader).
builder.Services.AddSingleton<IReadOnlyList<User>>(
    sp => DemoData.CreateUsers(sp.GetRequiredService<IPasswordHasher>()));
builder.Services.AddSingleton<IUserLookup>(
    sp => new InMemoryUserLookup(sp.GetRequiredService<IReadOnlyList<User>>()));
builder.Services.AddSingleton<IAccountReader>(
    sp => new InMemoryAccountReader(sp.GetRequiredService<IReadOnlyList<User>>()));
builder.Services.AddSingleton<LoginHandler>();

// ST-002 Sesija: neprozirni nasumični token, na serveru se čuva samo njegov heš.
builder.Services.AddSingleton(new SessionSettings(TimeSpan.FromHours(8)));
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<ISessionTokenProvider, SecureSessionTokenProvider>();
builder.Services.AddSingleton<ISessionIssuer, SessionIssuer>();
builder.Services.AddSingleton<ResolveSessionHandler>();

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
        return Results.Ok(new LoginResponse(
            result.Session!.Token, result.Session.ExpiresAt, result.Identity!));
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

// ST-002 Sesija: proverava token iz zaglavlja "Authorization: Bearer ...".
// Dijagnostički endpoint za proveru sesije;
app.MapGet("/api/auth/session", async (
    HttpRequest request,
    ResolveSessionHandler handler,
    CancellationToken cancellationToken) =>
{
    Guid? userId = await handler.HandleAsync(BearerToken.Read(request), cancellationToken);

    return userId is null
        ? Results.Json(new { error = "Sesija ne važi." },
            statusCode: StatusCodes.Status401Unauthorized)
        : Results.Ok(new { userId });
});
// ST-003 Aktivacija predmeta. Sme samo Administrator.
// Tok: token -> korisnik (sesija) -> uloga (AuthorizeRequestHandler) -> slučaj upotrebe.
app.MapPost("/courses/{id:guid}/activate", async (
    Guid id,
    HttpRequest request,
    ResolveSessionHandler sessions,
    AuthorizeRequestHandler authorization,
    ActivateCourseHandler handler,
    CancellationToken cancellationToken) =>
{
    Guid? userId = await sessions.HandleAsync(BearerToken.Read(request), cancellationToken);

    var decision = await authorization.HandleAsync(
        userId, new[] { UserRole.Administrator }, cancellationToken);

    if (decision == AccessDecision.Unauthenticated)
    {
        return Results.Json(new { error = "Niste prijavljeni." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    if (decision == AccessDecision.Forbidden)
    {
        return Results.Json(new { error = "Nemate pravo da aktivirate predmet." },
            statusCode: StatusCodes.Status403Forbidden);
    }

    var result = await handler.HandleAsync(id, cancellationToken);

    return result switch
    {
        ActivateCourseResult.NotFound =>
            Results.NotFound(new { error = "Predmet ne postoji." }),
        _ => Results.Ok(new { id, isActive = true, result = result.ToString() })
    };
});

app.Run();

/// <summary>HTTP ulaz prijave.</summary>
public sealed record LoginRequest(string? Username, string? Password);

/// <summary>HTTP izlaz uspešne prijave: token (vidi se samo ovde), rok isteka i identitet.</summary>
public sealed record LoginResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    AuthenticatedIdentity Identity);
