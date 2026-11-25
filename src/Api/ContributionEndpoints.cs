using Application.Contributions;

namespace Api;

public static class ContributionEndpoints
{
    public record ReviewRequest(string? Note);
    // PRIVREMENO: dok nema autentifikacije, pozivalac se prosledjuje kao userId i role.
    // Kad se uvede autentifikacija, Viewer treba da se cita iz identiteta.
    private static IResult Review(int userId, string role, Func<Viewer, ReviewResult> action)
    {
        if (!Enum.TryParse<ViewerRole>(role, ignoreCase: true, out var parsedRole))
        {
            return Results.BadRequest(new { error = "Uloga mora biti Student ili Teacher." });
        }

        var result = action(new Viewer(userId, parsedRole));

        return result.Success
            ? Results.Ok(new { message = result.Message })
            : Results.BadRequest(new { error = result.Message });
    }
    public static IEndpointRouteBuilder MapContributionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/contributions", (int userId, string role, ContributionQueries queries) =>
        {
            if (!Enum.TryParse<ViewerRole>(role, ignoreCase: true, out var parsedRole))
            {
                return Results.BadRequest(new { error = "Uloga mora biti Student ili Teacher." });
            }

            return Results.Ok(queries.List(new Viewer(userId, parsedRole)));
        });

        app.MapGet("/contributions/{id:int}", (int id, int userId, string role, ContributionQueries queries) =>
        {
            if (!Enum.TryParse<ViewerRole>(role, ignoreCase: true, out var parsedRole))
            {
                return Results.BadRequest(new { error = "Uloga mora biti Student ili Teacher." });
            }

            return queries.TryGetDetails(new Viewer(userId, parsedRole), id, out var dto, out var error)
                ? Results.Ok(dto)
                : Results.NotFound(new { error });
        });



        app.MapPost("/contributions/{id:int}/confirm",
            (int id, int userId, string role, ReviewRequest request, ContributionReviewHandler handler) =>
                Review(userId, role, viewer => handler.Confirm(viewer, id, request.Note ?? string.Empty)));

        app.MapPost("/contributions/{id:int}/return",
            (int id, int userId, string role, ReviewRequest request, ContributionReviewHandler handler) =>
                Review(userId, role, viewer => handler.ReturnForSupplement(viewer, id, request.Note ?? string.Empty)));

        return app;
    }
}
