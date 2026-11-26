using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Story "Jedan zapis po studentu i terminu": ponovljen unos ispravlja postojeći zapis.
/// </summary>
[TestFixture]
public sealed class OneRecordPerStudentTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();

    private static TeachingSession HeldSession()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        session.MarkHeld();
        return session;
    }

    private static RecordAttendanceHandler Handler(IAttendanceRepository attendance) =>
        new(new InMemoryEligibilityProvider().Add(OfferingId, GroupId, Ana), attendance);

    [Test]
    public async Task AnaOdsutanThenPrisutan_HasOneRecordWithPrisutan()
    {
        var session = HeldSession();
        var attendance = new InMemoryAttendanceRepository();
        var handler = Handler(attendance);

        await handler.HandleAsync(session, Ana, AttendanceState.Odsutan, Guid.NewGuid());
        await handler.HandleAsync(session, Ana, AttendanceState.Prisutan, Guid.NewGuid());

        var stored = await attendance.GetAsync(session.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Records, Has.Count.EqualTo(1));
            Assert.That(stored.StateOf(Ana), Is.EqualTo(AttendanceState.Prisutan));
        });
    }

    [Test]
    public async Task RepeatedEntry_KeepsSameRecordIdentity()
    {
        var session = HeldSession();
        var attendance = new InMemoryAttendanceRepository();
        var handler = Handler(attendance);

        await handler.HandleAsync(session, Ana, AttendanceState.Odsutan, Guid.NewGuid());
        var firstId = (await attendance.GetAsync(session.Id))!.Records.Single().Id;
        await handler.HandleAsync(session, Ana, AttendanceState.Prisutan, Guid.NewGuid());
        var secondId = (await attendance.GetAsync(session.Id))!.Records.Single().Id;

        Assert.That(secondId, Is.EqualTo(firstId));
    }

    [Test]
    public async Task RepeatedEntry_RecordsLatestActor()
    {
        var session = HeldSession();
        var attendance = new InMemoryAttendanceRepository();
        var handler = Handler(attendance);
        var secondActor = Guid.NewGuid();

        await handler.HandleAsync(session, Ana, AttendanceState.Odsutan, Guid.NewGuid());
        await handler.HandleAsync(session, Ana, AttendanceState.Prisutan, secondActor);

        var record = (await attendance.GetAsync(session.Id))!.Records.Single();
        Assert.That(record.ActorId, Is.EqualTo(secondActor));
    }
}
