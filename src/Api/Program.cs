using Application.Courses;
using Infrastructure.Demo;
using Application.CourseEnrollments;
using Infrastructure.CourseEnrollments;

var builder = WebApplication.CreateBuilder(args);

// Composition root
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// ST-006 Servisi
builder.Services.AddSingleton<ICourseEnrollmentRepository, InMemoryCourseEnrollmentRepository>();
builder.Services.AddScoped<ICourseExecutionPermissionChecker, AllowAllCourseExecutionPermissionChecker>();
builder.Services.AddScoped<EnrollStudentHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapGet("/courses", async (
    string? query,
    SearchCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(courses);
});

app.Run();
