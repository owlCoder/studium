using Application.Courses;
using Application.Reviews;
using Infrastructure.Demo;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();

// Registracija servisa za nacrt pregleda
builder.Services.AddScoped<UpdateReviewDraftHandler>();

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

// ST-014: Izmena sačuvanog nacrta pregleda
app.MapPut("/api/reviews/drafts/{id:guid}", async (
    Guid id,
    UpdateReviewDraftRequest request,
    UpdateReviewDraftHandler handler,
    CancellationToken cancellationToken) =>
{
    // Primer: ID nastavnika (u realnom sistemu stiže iz tokena)
    var teacherId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    var command = new UpdateReviewDraftCommand(
        DraftId: id,
        TeacherId: teacherId,
        Comment: request.Comment,
        Outcome: request.Outcome
    );

    try
    {
        await handler.HandleAsync(command, cancellationToken);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 403);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public record UpdateReviewDraftRequest(string Comment, string Outcome);