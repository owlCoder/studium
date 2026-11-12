using Api.Contracts;
using Application.Activities;
using Application.Courses;
using Infrastructure.Activities;
using Infrastructure.Demo;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// Memorijski adapter mora biti singleton da bi aktivnosti preživele između zahteva.
builder.Services.AddSingleton<IActivityRepository>(_ => DemoData.CreateActivityRepository());
builder.Services.AddSingleton<IActivityContextReader, InMemoryActivityContextReader>();
builder.Services.AddScoped<CreateActivityHandler>();

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

// Demonstracioni tok aktivnosti: memorijski adapter i bez autentifikacije.
// Ne predstavlja završenu funkcionalnost ST-021 niti proveru prava nad izvođenjem.
// Oznaka konteksta je oblika OZNAKA-xxxxx, npr. ERS-11111; razrešava je
// IActivityContextReader, pa se kasnije može zameniti oznakom izvođenja predmeta.
app.MapPost("/Course/{contextSlug}/CreateActivity", async (
    string contextSlug,
    CreateActivityRequest request,
    CreateActivityHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new CreateActivityCommand(
            contextSlug,
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate),
        cancellationToken);

    return result.Status switch
    {
        CreateActivityStatus.Created => Results.Created("/ListActivities", result.Activity),
        CreateActivityStatus.ContextNotFound => Results.Problem(
            title: "Predmet ili izvođenje nije pronađeno",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        CreateActivityStatus.InvalidRequest => Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["activity"] = new[] { result.Error ?? "Nevažeći podaci aktivnosti." }
            }),
        CreateActivityStatus.Conflict => Results.Problem(
            title: "Aktivnost već postoji",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        CreateActivityStatus.Canceled => Results.StatusCode(StatusCodes.Status499ClientClosedRequest),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();

// Potrebno za WebApplicationFactory u integracionim testovima.
public partial class Program
{
}
