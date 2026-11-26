using Application.Courses;
using Infrastructure.Demo;
using Application.Users;
using Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddSingleton<IUserRepository>(_ => new InMemoryUserRepository(DemoUsers.Seed()));
builder.Services.AddScoped<AdministratorGuard>();
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});
app.Run();

public partial class Program { }
