using Application.Courses;
using Application.Materials;
using Infrastructure.Demo;
using Infrastructure.Materials;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// ST-009: memorijsko skladište materijala do odluke o bazi (ADR).
builder.Services.AddSingleton<IMaterialRepository, InMemoryMaterialRepository>();
builder.Services.AddScoped<CreateMaterialHandler>();

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

// ST-009: kreiranje materijala kao nacrta na konkretnom izvođenju.
// Provera nastavnog osoblja izvođenja (ST-005) još NIJE dodata.
app.MapPost("/offerings/{offeringId:guid}/materials", async (
    Guid offeringId,
    CreateMaterialRequest request,
    CreateMaterialHandler handler,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = ["Naziv materijala je obavezan."]
        });
    }

    try
    {
        var id = await handler.HandleAsync(
            new CreateMaterialCommand(offeringId, request.Name, request.Description),
            cancellationToken);

        return Results.Created(
            $"/offerings/{offeringId}/materials/{id}",
            new CreateMaterialResponse(id, "Draft"));
    }
    catch (ArgumentException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [ex.ParamName ?? "request"] = [ex.Message]
        });
    }
});

app.Run();

internal sealed record CreateMaterialRequest(string? Name, string? Description);

internal sealed record CreateMaterialResponse(Guid Id, string PublicationState);
