using System;
using System.Security.Claims;
using Api.Authentication;
using Application.CourseEnrollments;
using Application.Courses;
using Application.Interfaces;
using Application.Services;
using Infrastructure.CourseEnrollments;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();


builder.Services.AddSingleton(_ => new InMemoryCourseEnrollmentRepository(DemoData.CreateEnrollments()));
builder.Services.AddSingleton<IEnrollmentRepository>(sp => sp.GetRequiredService<InMemoryCourseEnrollmentRepository>());
builder.Services.AddSingleton<ICourseEnrollmentRepository>(sp => sp.GetRequiredService<InMemoryCourseEnrollmentRepository>());
builder.Services.AddSingleton<IOfferingAccessReader>(_ => DemoData.CreateOfferingAccessReader());
builder.Services.AddScoped<EnrollmentService>();
builder.Services.AddScoped<EnrollStudentHandler>();


builder.Services
    .AddAuthentication(DemoAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>(DemoAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


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
    var teacherIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
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
