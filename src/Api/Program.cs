using System.Security.Claims;
using Api.Security;
using Application.Access;
using Application.Auth;
using Application.Courses;
using Application.Users;
using Domain.Users;
using Infrastructure.Demo;
using Infrastructure.Security;
using Infrastructure.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

const string AdministratorPolicy = "ActiveAdministrator";

// --------------------------------------------------
// 1. Registracija postojećih funkcionalnosti kurseva
// --------------------------------------------------

builder.Services.AddSingleton<ICourseReader>(
    _ => DemoData.CreateCourseReader());

builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// --------------------------------------------------
// 2. Jedan zajednički korisnički repozitorijum
// --------------------------------------------------

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

builder.Services.AddSingleton<InMemoryUserRepository>(sp =>
{
    if (builder.Environment.IsDevelopment())
    {
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        return DemoData.CreateUserRepository(hasher);
    }

    return new InMemoryUserRepository();
});

// Sva tri interfejsa koriste istu instancu.
builder.Services.AddSingleton<IUserRepository>(
    sp => sp.GetRequiredService<InMemoryUserRepository>());

builder.Services.AddSingleton<IUserLookup>(
    sp => sp.GetRequiredService<InMemoryUserRepository>());

builder.Services.AddSingleton<IAccountReader>(
    sp => sp.GetRequiredService<InMemoryUserRepository>());

// --------------------------------------------------
// 3. Application handler-i
// --------------------------------------------------

builder.Services.AddSingleton<LoginHandler>();
builder.Services.AddScoped<DeactivateUserHandler>();

// --------------------------------------------------
// 4. Cookie autentifikacija
// --------------------------------------------------

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "studium.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        // API vraća 401 umesto HTML redirect-a.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };

        // API vraća 403 umesto HTML redirect-a.
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };
    });

// --------------------------------------------------
// 5. Serverska administratorska autorizacija
// --------------------------------------------------

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdministratorPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ActiveAdministratorRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler,
    ActiveAdministratorAuthorizationHandler>();

var app = builder.Build();

// Middleware mora biti pre zaštićenih ruta.
app.UseAuthentication();
app.UseAuthorization();

// --------------------------------------------------
// 6. Javni endpoint-i
// --------------------------------------------------

app.MapGet("/health", () =>
    Results.Ok(new { status = "ok" }));

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

// --------------------------------------------------
// 7. Prijava: provera lozinke i izdavanje Cookie-a
// --------------------------------------------------

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    LoginHandler handler,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new LoginCommand(request.Username, request.Password),
        cancellationToken);

    if (!result.Succeeded)
    {
        if (result.Failure == LoginFailure.InvalidInput)
        {
            return Results.BadRequest(new
            {
                error = "Korisničko ime i lozinka su obavezni."
            });
        }

        return Results.Json(
            new { error = "Neuspešna prijava." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var identity = result.Identity!;

    // Cookie sadrži identitet, ne podatke kojima klijent određuje ulogu.
    var claims = new[]
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            identity.UserId.ToString()),

        new Claim(
            ClaimTypes.Name,
            identity.Username)
    };

    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme));

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal);

    return Results.Ok(new
    {
        identity.UserId,
        identity.Username,
        identity.Role
    });
})
.AllowAnonymous();

// --------------------------------------------------
// 8. Deaktivacija: isključivo aktivni administrator
// --------------------------------------------------

app.MapPut("/users/{id}/deactivate", async (
    Guid id,
    DeactivateUserHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(id, cancellationToken);

    return result switch
    {
        DeactivateUserResult.Deactivated =>
            Results.NoContent(),

        DeactivateUserResult.NotFound =>
            Results.NotFound(new
            {
                message = "Korisnik nije pronađen."
            }),

        DeactivateUserResult.AlreadyDeactivated =>
            Results.Conflict(new
            {
                message = "Nalog je već deaktiviran."
            }),

        DeactivateUserResult.LastActiveAdministrator =>
            Results.Conflict(new
            {
                message = "Nije moguće deaktivirati poslednjeg aktivnog administratora."
            }),

        _ => Results.StatusCode(
            StatusCodes.Status500InternalServerError)
    };
})
.RequireAuthorization(AdministratorPolicy);

app.Run();

public sealed record LoginRequest(string? Username, string? Password);

public partial class Program { }
