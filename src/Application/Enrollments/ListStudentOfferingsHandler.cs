namespace Application.Enrollments;

public sealed class ListStudentOfferingsHandler
{
    private readonly IStudentOfferingsReader _reader;

    public ListStudentOfferingsHandler(IStudentOfferingsReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<EnrolledOfferingDto>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        }

        var offerings = await _reader.ListActiveForStudentAsync(studentId, cancellationToken);

        return offerings
            .DistinctBy(offering => offering.OfferingId)
            .OrderByDescending(offering => offering.AcademicYear, StringComparer.Ordinal)
            .ThenBy(offering => offering.CourseName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
