using Application.Courses;
using Infrastructure.Demo;
using Studium.Application.Reviews;
using Studium.Domain.Reviews;
using Studium.Infrastructure.Demo;

var builder = WebApplication.CreateBuilder(args);

// Composition root: registracija zavisnosti
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();

// ST-014: Registracija repozitorijuma i aplikativnog handlera
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

// ST-014: Izmena nacrta pregleda
// NAPOMENA / INTEGRACIONA BLOKADA:
// HTTP REST route handler (PUT /api/reviews/drafts/{id}) je izostavljen iz API sloja
// dok se ne implementira verifikovani autentifikacioni/autorizacioni mehanizam.
// Kompletna poslovna logika i ugovori nalaze se u Application i Domain slojevima.

app.Run();

namespace Studium.Infrastructure.Demo
{
    using Studium.Domain.Reviews;

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