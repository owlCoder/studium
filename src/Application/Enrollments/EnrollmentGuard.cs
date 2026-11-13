using Domain.Enrollments;

namespace Application.Enrollments;

/// <summary>
/// Guard koji sprečava predaju radova studentu bez aktivnog članstva (ST-006).
/// </summary>
public sealed class EnrollmentGuard
{
    private readonly IEnrollmentReader _reader;

    public EnrollmentGuard(IEnrollmentReader reader)
        => _reader = reader;

    /// <summary>
    /// Proverava da student ima aktivno članstvo pre predaje rada.
    /// Baca <see cref="EnrollmentEndedException"/> ako članstvo ne postoji ili je prekinuto.
    /// </summary>
    public async Task EnsureCanSubmitWorkAsync(
        Guid courseOfferingId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var active = await _reader.GetActiveAsync(
            courseOfferingId, studentId, cancellationToken);

        if (active is null || !active.IsActive)
        {
            throw new EnrollmentEndedException(
                "Student nema aktivno članstvo na ovom izvođenju predmeta – predaja rada nije dozvoljena.");
        }
    }
}
