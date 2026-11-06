using Studium.Application.Reviews;
using Studium.Domain.Reviews;
using Studium.Infrastructure.Demo;
using Infrastructure.Demo;
using Application.Courses;

var builder = WebApplication.CreateBuilder(args);

// Composition root: registracija zavisnosti
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();

builder.Services.AddSingleton<IReviewRepository, InMemoryReviewRepository>();
builder.Services.AddScoped<UpdateReviewDraftHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.MapPut("/api/reviews/drafts/{id:guid}", async (
    Guid id,
    UpdateReviewDraftRequest request,
    UpdateReviewDraftHandler handler,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    // Citanje Teacher ID-ja iz HTTP headera 'X-Teacher-Id' umesto laznog autentifikatora
    if (!httpContext.Request.Headers.TryGetValue("X-Teacher-Id", out var teacherIdHeader) ||
        !Guid.TryParse(teacherIdHeader, out var teacherId))
    {
        return Results.Json(new { error = "Nedostaje ili je nevalidno zaglavlje X-Teacher-Id." }, statusCode: 401);
    }

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

// In-Memory Repository implementacija da API ne puca pri pokretanju (Rešava Tačku 2)
namespace Studium.Infrastructure.Demo
{
    public class InMemoryReviewRepository : IReviewRepository
    {
        private readonly List<ReviewDraft> _drafts = new();

        public Task<ReviewDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var draft = _drafts.FirstOrDefault(d => d.Id == id);
            return Task.FromResult(draft);
        }

        public Task UpdateAsync(ReviewDraft draft, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}