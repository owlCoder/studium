using System;
using Application.Courses;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Demo;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<EnrollmentService>();
builder.Services.AddControllers();
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Javno dostupan demonstracioni endpoint, bez autentifikacije:
// ne predstavlja funkcionalnost ST-003 i ne koristiti ga za stvarne podatke.
app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.Run();

