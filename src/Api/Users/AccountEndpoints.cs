using Application.Sessions;
using Application.Users;

namespace Api.Users;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        // Odjava: poništava sesije prijavljenog korisnika.
        app.MapPost("/api/auth/logout", async (
            HttpRequest request,
            ResolveSessionHandler sessions,
            LogoutHandler handler,
            CancellationToken cancellationToken) =>
        {
            Guid? userId = await sessions.HandleAsync(BearerToken.Read(request), cancellationToken);
            if (userId is null)
                return Unauthenticated();

            await handler.HandleAsync(userId.Value, cancellationToken);
            return Results.NoContent();
        });

        // Promena SOPSTVENE lozinke: traži tačnu postojeću lozinku.
        app.MapPost("/api/auth/change-password", async (
            ChangePasswordRequest body,
            HttpRequest request,
            ResolveSessionHandler sessions,
            ChangePasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            Guid? userId = await sessions.HandleAsync(BearerToken.Read(request), cancellationToken);
            if (userId is null)
                return Unauthenticated();

            var result = await handler.HandleAsync(
                userId.Value,
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
