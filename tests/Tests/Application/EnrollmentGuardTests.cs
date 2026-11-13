using Application.Enrollments;
using Domain.Enrollments;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class EnrollmentGuardTests
{
    // ---- Pomoćne metode ----

    private static Enrollment Active(Guid offeringId, Guid studentId) =>
        new(Guid.NewGuid(), studentId, offeringId, DateTimeOffset.UtcNow);

    private static Enrollment Ended(Guid offeringId, Guid studentId)
    {
        var e = Active(offeringId, studentId);
        e.End();
        return e;
    }

    private sealed class FakeReader : IEnrollmentReader
    {
        private readonly Enrollment? _enrollment;
        public FakeReader(Enrollment? enrollment = null) => _enrollment = enrollment;

        public Task<Enrollment?> GetActiveAsync(
            Guid courseOfferingId, Guid studentId, CancellationToken ct = default)
        {
            if (_enrollment is null || !_enrollment.IsActive)
                return Task.FromResult<Enrollment?>(null);

            return Task.FromResult<Enrollment?>(_enrollment);
        }
    }

    // ---- testovi ----

    [Test]
    public void EnsureCanSubmitWork_ActiveEnrollment_Allows()   // ← uklonjen async
    {
        var offeringId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var guard = new EnrollmentGuard(new FakeReader(Active(offeringId, studentId)));

        Assert.DoesNotThrowAsync(() =>
            guard.EnsureCanSubmitWorkAsync(offeringId, studentId));
    }

    [Test]
    public void EnsureCanSubmitWork_EndedEnrollment_Throws()
    {
        var offeringId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var guard = new EnrollmentGuard(new FakeReader(Ended(offeringId, studentId)));

        var ex = Assert.ThrowsAsync<EnrollmentEndedException>(() =>
            guard.EnsureCanSubmitWorkAsync(offeringId, studentId));

        Assert.That(ex!.Message, Does.Contain("predaja rada nije dozvoljena"));
    }

    [Test]
    public void EnsureCanSubmitWork_NoEnrollment_Throws()
    {
        var guard = new EnrollmentGuard(new FakeReader(null));

        Assert.ThrowsAsync<EnrollmentEndedException>(() =>
            guard.EnsureCanSubmitWorkAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
