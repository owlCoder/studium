using Application.Courses;
using Infrastructure.Demo;
using Studium.Application;
using Studium.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();

// ST-014 (KT1): Registracija repozitorijuma i handlera za nacrt pregleda
builder.Services.AddSingleton<IReviewRepository, InMemoryReviewRepository>();
builder.Services.AddScoped<SaveReviewDraftHandler>();

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

// ST-014 (KT1: domen + aplikativni sloj + in-memory adapter):
// HTTP PUT ruta nije izložena u ovoj fazi dok se ne implementira verifikovana autentifikacija/autorizacija.

app.Run();