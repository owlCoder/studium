using Application.Courses;
using Infrastructure.Demo;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
builder.Services.AddScoped<Application.Courses.GetSubmissionHandler>();

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

// ST-020: Endpoint za dohvat predaje uz očuvanje pristupa autoru rada
app.MapGet("/submissions/{submissionId}", async (
    string submissionId,
    string studentId,
    Application.Courses.GetSubmissionHandler handler,
    CancellationToken cancellationToken) =>
{
    var submission = await handler.HandleAsync(studentId, submissionId, cancellationToken);

    if (submission is null)
    {
        return Results.Forbid();
    }

    return Results.Ok(submission);
});

app.Run();
