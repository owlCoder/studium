using Application.Activities;
using Application.Common;
using Application.Courses;
using Domain.Offerings;

namespace Infrastructure.Activities;

/// <summary>
/// Implementacija porta <see cref="IActivityContextReader"/> koja aktivnosti vezuje za
/// izvođenje predmeta (ST-004), a ne za kataloški predmet. Oznaka sadrži i akademsku
/// godinu, jer pet znakova identifikatora nije dovoljno za jedinstvenost:
/// <c>OZNAKA-GODINA-xxxxx</c>, gde je <c>GODINA</c> akademska godina izvođenja
/// (npr. ERS 2026/27 sa identifikatorom
/// <c>33333333-0000-0000-0000-000000000001</c> daje <c>ERS-2026-27-00001</c>, a isto
/// izvođenje iz 2025/26 daje <c>ERS-2025-26-00003</c>). Kako par (predmet, godina)
/// jednoznačno određuje izvođenje, dve godine istog predmeta više ne mogu da se sudare.
/// Aktivnost tada čuva identifikator izvođenja u <see cref="ActivityContext.Id"/>.
/// Adapter ne uvodi nove portove: spisak izvođenja dobija kroz konstruktor, pa ga
/// ST-004 kasnije napaja iz svog skladišta bez izmene ovog koda. Neuspesi se vraćaju
/// kao status, bez izuzetaka.
/// </summary>
public sealed class OfferingActivityContextReader : IActivityContextReader
{
    private readonly ICourseReader _courseReader;
    private readonly IReadOnlyList<Offering> _offerings;

    public OfferingActivityContextReader(ICourseReader courseReader, IEnumerable<Offering> offerings)
    {
        _courseReader = courseReader;
        _offerings = offerings.ToList();
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

            var matches = _offerings
                .Select(offering => new
                {
                    Offering = offering,
                    Prefix = BuildPrefix(
                        courses.FirstOrDefault(course => course.Id == offering.CourseId),
                        offering)
                })
                .Where(candidate => candidate.Prefix is not null
                    && EntitySlug.Matches(contextSlug, candidate.Prefix, candidate.Offering.Id))
                .Select(candidate => candidate.Offering)
                .ToList();

            return matches.Count switch
            {
                0 => ActivityContextLookup.NotFound(
                    $"Izvođenje sa oznakom \"{contextSlug}\" nije pronađeno."),
                > 1 => ActivityContextLookup.Ambiguous(
                    $"Oznaka \"{contextSlug}\" nije jedinstvena."),
                _ => ActivityContextLookup.Found(new ActivityContext(matches[0].Id))
            };
        }
        catch (OperationCanceledException)
        {
            return ActivityContextLookup.Canceled();
        }
    }

    /// <summary>
    /// Prefiks oznake: oznaka predmeta i akademska godina izvođenja. Normalizacija u
    /// <see cref="EntitySlug"/> pretvara <c>/</c> u <c>-</c>, pa 2026/27 postaje 2026-27.
    /// </summary>
    private static string? BuildPrefix(Domain.Courses.Course? course, Offering offering) =>
        course is null ? null : $"{course.Code}-{offering.AcademicYear}";
}
