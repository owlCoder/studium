using Application.Courses;
using Application.Users;
using Infrastructure.Demo;
using Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<DeactivateUserHandler>();

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

app.MapPut("/users/{id}/deactivate", async (
    Guid id,
    DeactivateUserHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        await handler.HandleAsync(id, cancellationToken);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
});

app.Run();

public partial class Program { }
