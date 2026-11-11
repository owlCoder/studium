using Domain.Enrollments;

namespace Application.Enrollments;

/// <summary>
/// Apstrakcija za čitanje članstva — omogućava guard-u da ne zavisi
/// od konkretnog repozitorijuma (koji još nije finalizovan).
/// </summary>
public interface IEnrollmentReader
{
    Task<Enrollment?> GetActiveAsync(
        Guid courseOfferingId,
        Guid studentId,
        CancellationToken cancellationToken = default);
}
