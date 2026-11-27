using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Prelazak studenta iz V1 u V2 (primer iz ST-029): pre prelaska je na spisku V1,
/// a posle prelaska na spisku V2.
/// </summary>
[TestFixture]
public sealed class EligibilityTransferTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly Guid V2 = Guid.NewGuid();
    private static readonly DateTime TransferAt = new(2026, 11, 10, 0, 0, 0);

    private static readonly Guid Ana = Guid.NewGuid();

    private static InMemoryEligibilityProvider ProviderWithTransfer() =>
        new InMemoryEligibilityProvider()
            .Add(OfferingId, V1, Ana, new MembershipPeriod(new DateTime(2026, 10, 1), TransferAt))
            .Add(OfferingId, V2, Ana, new MembershipPeriod(TransferAt));

    private static TeachingSession HeldSession(Guid groupId, DateTime start)
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, groupId,
            start, start.AddMinutes(90), "Učionica 101");
        session.MarkHeld();
        return session;
    }

    private static async Task<IEnumerable<Guid>> EligibleFor(TeachingSession session)
    {
        var list = new ListEligibleStudentsHandler(
            ProviderWithTransfer(), new InMemoryAttendanceRepository());
        var result = await list.HandleAsync(session);
        return result.Select(s => s.StudentId);
    }

    [Test]
    public async Task BeforeTransfer_StudentIsOnV1List()
    {
        var result = await EligibleFor(HeldSession(V1, new DateTime(2026, 11, 7, 10, 0, 0)));

        Assert.That(result, Does.Contain(Ana));
    }

    [Test]
    public async Task BeforeTransfer_StudentIsNotOnV2List()
    {
        var result = await EligibleFor(HeldSession(V2, new DateTime(2026, 11, 7, 12, 0, 0)));

        Assert.That(result, Does.Not.Contain(Ana));
    }

    [Test]
    public async Task AfterTransfer_StudentIsOnV2List()
    {
        var result = await EligibleFor(HeldSession(V2, new DateTime(2026, 11, 14, 10, 0, 0)));

        Assert.That(result, Does.Contain(Ana));
    }

    [Test]
    public async Task AfterTransfer_StudentIsNotOnV1List()
    {
        var result = await EligibleFor(HeldSession(V1, new DateTime(2026, 11, 14, 10, 0, 0)));

        Assert.That(result, Does.Not.Contain(Ana));
    }
}
