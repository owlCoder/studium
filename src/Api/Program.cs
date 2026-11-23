using System;
using Api.Authentication;
using Application.Courses;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Demo;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Application.Submissions;
using Infrastructure.Submissions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
builder.Services.AddSingleton<IEnrollmentRepository>(
    _ => new EnrollmentRepository(DemoData.CreateEnrollments()));
builder.Services.AddSingleton<IOfferingAccessReader>(_ => DemoData.CreateOfferingAccessReader());
builder.Services.AddScoped<EnrollmentService>();


builder.Services
    .AddAuthentication(DemoAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>(DemoAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ISubmissionRepository, InMemorySubmissionRepository>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentStudent>();
builder.Services.AddScoped<GetOwnSubmissionHandler>();

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

if (DemoAuthHandler.IsAllowedEnvironment(app.Environment) &&
    app.Configuration.GetValue<bool>("DemoSubmissions"))
{
    await app.Services.GetRequiredService<ISubmissionRepository>()
        .AddAsync(SubmissionDemoData.CreateSubmission());
}

app.Run();


public partial class Program { }
