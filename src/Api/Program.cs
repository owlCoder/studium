using Application.Courses;
using Application.Users;
using Infrastructure.Demo;
using Domain.Users;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));


app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.MapPut("/users/{id}/deactivate", async (Guid id, DeactivateUserHandler handler, CancellationToken cancellationToken) =>
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
