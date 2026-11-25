using System.Security.Claims;
using Api.Identity;
using Application.Engagements;
using Application.Enrollments;
using Application.Home;
using Domain.Dashboard;

namespace Api.Home;

/// <summary>
/// HTTP endpointi ST-007 (početni pregled korisnika). Poslovna pravila su u slučajevima upotrebe
/// (Application); endpointi samo čitaju identitet iz sesije, pozivaju slučaj upotrebe i prevode
/// ishod u HTTP status.
/// Identitet se uzima isključivo iz sesije (US-7.5, politika <see cref="SessionOnlyPolicy"/>),
/// nikad iz zahteva; bez važeće sesije i aktivnog naloga zahtev se odbija sa 401.
/// </summary>
public static class HomeEndpoints
{
    /// <summary>Politika koja za ST-007 endpointe prihvata samo prijavu sesijom (Bearer token).</summary>
    public const string SessionOnlyPolicy = "StudiumSessionOnly";

    public static IEndpointRouteBuilder MapHomeEndpoints(this IEndpointRouteBuilder app)
    {
        // US-7.1: izvođenja na koja je student upisan.
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
        }).RequireAuthorization(SessionOnlyPolicy);

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
        }).RequireAuthorization(SessionOnlyPolicy);

        // US-7.4: ceo početni pregled u jednom JSON odgovoru, bez keširanja.
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
        }).RequireAuthorization(SessionOnlyPolicy);

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
        }).RequireAuthorization(SessionOnlyPolicy);

        // US-7.10: otvaranje veze ka delu predmeta (zadaci, materijali, obaveštenja).
        // Samo deo koji registar (US-7.6) označava kao dostupan, i samo za korisnika sa pristupom izvođenju.
        app.MapGet("/offerings/{offeringId:guid}/{area:alpha}", async (
            Guid offeringId,
            string area,
            ClaimsPrincipal user,
            CheckContentAccessHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!user.TryGetSessionUserId(out var userId))
            {
                return Results.Unauthorized();
            }

            var content = await handler.ForAreaAsync(userId, offeringId, area, cancellationToken);
            return content is null ? Results.NotFound() : Results.Ok(content);
        }).RequireAuthorization(SessionOnlyPolicy);

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
        }).RequireAuthorization(SessionOnlyPolicy);

        // US-7.5: vraća ID prijavljenog korisnika, uzet isključivo iz sesije.
        app.MapGet("/api/ja", (ClaimsPrincipal user) =>
                user.TryGetSessionUserId(out var userId)
                    ? Results.Ok(new { userId })
                    : Results.Unauthorized())
            .RequireAuthorization(SessionOnlyPolicy);

        // US-7.6: lista podržanih vrsta aktivnosti i funkcionalnih oblasti.
        app.MapGet("/features", (FeatureRegistry registry) => Results.Ok(new
        {
            activityTypes = registry.SupportedActivityTypes
                .Select(type => new { code = type.Code, area = type.Area.ToString() }),
            areas = registry.AvailableAreas.Select(area => area.ToString())
        }));

        // US-7.6: provera da li je vrsta aktivnosti podržana.
        app.MapGet("/features/activity-types/{activityType}", (string activityType, FeatureRegistry registry) =>
            Results.Ok(new { activityType, supported = registry.IsActivitySupported(activityType) }));

        return app;
    }
}
