using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Enrollments;

namespace Application.Engagements;

/// <summary>
/// US-7.2: izvođenja na kojima je prijavljeni korisnik angažovan, i izvođenja na koja je
/// upisan kao student (upit iz US-7.1). ID korisnika dolazi iz sesije (US-7.5), nikada iz zahteva.
/// </summary>
public sealed class ListMyOfferingsHandler
{
    private readonly IStaffOfferingsReader _staffReader;
    private readonly IStudentOfferingsReader _studentReader;

    public ListMyOfferingsHandler(
        IStaffOfferingsReader staffReader,
        IStudentOfferingsReader studentReader)
    {
        _staffReader = staffReader;
        _studentReader = studentReader;
    }

    public async Task<MyOfferingsDto> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }

        var staffRows = await _staffReader.ListActiveForUserAsync(userId, cancellationToken);
        var studentRows = await _studentReader.ListActiveForStudentAsync(userId, cancellationToken);

        // Više aktivnih angažovanja na istom izvođenju daje jedan red sa najvišom ulogom
        // (ResponsibleTeacher, pa Teacher, pa Assistant: redosled vrednosti u enumu).
        var teaching = staffRows
            .GroupBy(row => row.OfferingId)
            .Select(group => group.OrderBy(row => row.Role).First())
            .OrderByDescending(row => row.AcademicYear, StringComparer.Ordinal)
            .ThenBy(row => row.CourseName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Bez duplikata: izvođenje na kom korisnik predaje ide samo u „Predajem”.
        var teachingIds = teaching.Select(row => row.OfferingId).ToHashSet();

        var enrolled = studentRows
            .Where(row => !teachingIds.Contains(row.OfferingId))
            .DistinctBy(row => row.OfferingId)
            .OrderByDescending(row => row.AcademicYear, StringComparer.Ordinal)
            .ThenBy(row => row.CourseName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MyOfferingsDto(teaching, enrolled);
    }


}
