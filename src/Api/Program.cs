using Application.Courses;
using Infrastructure.Demo;
using Application.CourseEnrollments;
using Infrastructure.CourseEnrollments;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();

builder.Services.AddSingleton<ICourseEnrollmentRepository, InMemoryCourseEnrollmentRepository>();
builder.Services.AddScoped<EnrollStudentHandler>();

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

app.MapPost("/api/enrollments", async (
    EnrollStudentCommand command,
    EnrollStudentHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var enrollmentId = await handler.HandleAsync(command, cancellationToken);
        return Results.Created($"/api/enrollments/{enrollmentId}", new { id = enrollmentId });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.Run();
