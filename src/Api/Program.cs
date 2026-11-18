using System;
using System.Security.Claims;
using Api.Authentication;
using Application.Access;
using Application.Assignments;
using Application.Courses;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Assignments;
using Infrastructure.Demo;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
builder.Services.AddSingleton<IEnrollmentRepository>(
    _ => new EnrollmentRepository(DemoData.CreateEnrollments()));
builder.Services.AddSingleton<IOfferingAccessReader>(_ => DemoData.CreateOfferingAccessReader());
builder.Services.AddScoped<EnrollmentService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<InMemoryAssignmentDraftWriter>();
builder.Services.AddSingleton<IAssignmentDraftWriter>(
    services => services.GetRequiredService<InMemoryAssignmentDraftWriter>());
builder.Services.AddSingleton<IAssignmentRepository>(
    services => services.GetRequiredService<InMemoryAssignmentDraftWriter>());
builder.Services.AddScoped<IAssignmentCreationAccess>(services =>
{
    var user = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.User;
    UserContext? currentUser = null;
    if (user?.Identity?.IsAuthenticated == true &&
        user.IsInRole(nameof(UserRole.Teacher)) &&
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) &&
        userId != Guid.Empty)
    {
        currentUser = new UserContext(userId, UserRole.Teacher);
    }

    return new AssignmentCreationAccess(
        currentUser, services.GetRequiredService<IOfferingAccessReader>());
});
builder.Services.AddScoped<CreateAssignmentHandler>();

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

app.MapGet("/courses", async (
    string? query,
    SearchCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(courses);
});

app.Run();


public partial class Program { }
