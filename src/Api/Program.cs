using System.Security.Claims;
using Application.Courses;
using Infrastructure.Demo;
using Application.CourseEnrollments;
using Infrastructure.CourseEnrollments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();


builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();


builder.Services.AddSingleton<ICourseEnrollmentRepository, InMemoryCourseEnrollmentRepository>();
builder.Services.AddScoped<ICourseExecutionPermissionChecker, DenyAllCourseExecutionPermissionChecker>();
builder.Services.AddScoped<EnrollStudentHandler>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.MapPost("/api/enrollments", async (
    EnrollStudentRequest request,
    ClaimsPrincipal user,
    EnrollStudentHandler handler,
    CancellationToken cancellationToken) =>
{
    var teacherIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(teacherIdClaim, out var verifiedTeacherId) || verifiedTeacherId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    try
    {
        
        var command = new EnrollStudentCommand(verifiedTeacherId, request.CourseExecutionId, request.StudentId);
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
})
.RequireAuthorization();

app.MapGet("/courses", async (
    string? query,
    SearchCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(courses);
});

app.Run();


public record EnrollStudentRequest(Guid CourseExecutionId, Guid StudentId);


public partial class Program { }
