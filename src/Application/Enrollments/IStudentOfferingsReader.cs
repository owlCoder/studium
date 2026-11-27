namespace Application.Enrollments;

public interface IStudentOfferingsReader
{
    Task<IReadOnlyList<EnrolledOfferingDto>> ListActiveForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
