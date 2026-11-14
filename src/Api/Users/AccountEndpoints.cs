using Application.Sessions;
using Application.Users;

namespace Api.Users;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        // Odjava: poništava samo sesiju iz tokena kojim je poslat zahtev.
        app.MapPost("/api/auth/logout", async (
            HttpRequest request,
            ResolveSessionHandler sessions,
            LogoutHandler handler,
            CancellationToken cancellationToken) =>
        {
            string? token = BearerToken.Read(request);
            Guid? userId = await sessions.HandleAsync(token, cancellationToken);
            if (userId is null)
                return Unauthenticated();

            await handler.HandleAsync(token, cancellationToken);
            return Results.NoContent();
        });

        // Promena SOPSTVENE lozinke: traži tačnu postojeću lozinku.
        // Ostale sesije korisnika se poništavaju, a ova ostaje važeća.
        app.MapPost("/api/auth/change-password", async (
            ChangePasswordRequest body,
            HttpRequest request,
            ResolveSessionHandler sessions,
            ChangePasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            string? token = BearerToken.Read(request);
            Guid? userId = await sessions.HandleAsync(token, cancellationToken);
            if (userId is null)
                return Unauthenticated();

            var result = await handler.HandleAsync(
                userId.Value,
                token!,
                body.CurrentPassword ?? string.Empty,
                body.NewPassword ?? string.Empty,
                cancellationToken);

            return result switch
            {
                ChangePasswordResult.Success => Results.NoContent(),
                ChangePasswordResult.InvalidCurrentPassword =>
                    Results.BadRequest(new { error = "Postojeća lozinka nije tačna." }),
                ChangePasswordResult.InvalidNewPassword =>
                    Results.BadRequest(new { error = "Nova lozinka nije prihvatljiva." }),
                _ => Unauthenticated()
            };
        });

        return app;
    }

    private static IResult Unauthenticated()
        => Results.Json(new { error = "Niste prijavljeni." },
            statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>HTTP ulaz promene lozinke. Namerno bez ID-a korisnika.</summary>
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
