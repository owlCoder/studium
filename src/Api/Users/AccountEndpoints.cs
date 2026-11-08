using System.Security.Claims;
using Application.Users;

namespace Api.Users;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        // Odjava: poništava sesiju prijavljenog korisnika.
        app.MapPost("/api/auth/logout", async (
            ClaimsPrincipal principal,
            LogoutHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Unauthenticated();

            await handler.HandleAsync(userId, cancellationToken);
            return Results.NoContent();
        });

        // Promena SOPSTVENE lozinke: traži tačnu postojeću lozinku.
        app.MapPost("/api/auth/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            ChangePasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Unauthenticated();

            var result = await handler.HandleAsync(
                userId,
                request.CurrentPassword ?? string.Empty,
                request.NewPassword ?? string.Empty,
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

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    private static IResult Unauthenticated()
        => Results.Json(new { error = "Niste prijavljeni." },
            statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>HTTP ulaz promene lozinke. Namerno bez ID-a korisnika.</summary>
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);