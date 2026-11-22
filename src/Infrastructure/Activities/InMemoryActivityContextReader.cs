using Application.Activities;
using Application.Common;
using Application.Courses;
using Domain.Courses;

namespace Infrastructure.Activities;

/// <summary>
/// Privremena implementacija porta <see cref="IActivityContextReader"/>: oznaka
/// oblika <c>OZNAKA-xxxxx</c> razrešava se na kataloški predmet preko
/// <see cref="ICourseReader"/>. Kada ST-004 uvede izvođenja predmeta, ovaj adapter
/// se zamenjuje implementacijom koja razrešava oznaku izvođenja; domen, port
/// repozitorijuma i slučajevi upotrebe ostaju nepromenjeni.
/// Neuspesi se vraćaju kao status, bez izuzetaka.
/// </summary>
public sealed class InMemoryActivityContextReader : IActivityContextReader
{
    private readonly ICourseReader _courseReader;

    public InMemoryActivityContextReader(ICourseReader courseReader)
    {
        _courseReader = courseReader;
    }

    public async Task<ActivityContextLookup> ResolveAsync(
        string? contextSlug,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ActivityContextLookup.Canceled();
        }

        try
        {
            var courses = await _courseReader.ListAsync(cancellationToken);
            var matches = courses
                .Where(course => EntitySlug.Matches(contextSlug, course.Code, course.Id))
                .ToList();

            return matches.Count switch
            {
                0 => ActivityContextLookup.NotFound(
                    $"Kontekst \"{contextSlug}\" nije pronađen."),
                > 1 => ActivityContextLookup.Ambiguous(
                    $"Oznaka \"{contextSlug}\" nije jedinstvena."),
                _ => ActivityContextLookup.Found(ToContext(matches[0]))
            };
        }
        catch (OperationCanceledException)
        {
            return ActivityContextLookup.Canceled();
        }
    }

    private static ActivityContext ToContext(Course course) =>
        new(course.Id);
}
