using Application.Contributions;

namespace Api;

public static class ContributionEndpoints
{
    // PRIVREMENO: dok nema autentifikacije, pozivalac se prosledjuje kao userId i role.
    // Kad se uvede autentifikacija, Viewer treba da se cita iz identiteta.
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

        return app;
    }
}
