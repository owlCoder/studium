using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Acceptance;

/// <summary>
/// Test prihvatanja za ST-032, drugi deo primera iz specifikacije:
/// "Ponovljen unos za Anu menja isti zapis."
/// </summary>
[TestFixture]
public sealed class St032RepeatedEntryAcceptanceTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();

    private TeachingSession _session = null!;
    private InMemoryAttendanceRepository _attendance = null!;
    private RecordAttendanceHandler _record = null!;

    [SetUp]
    public void SetUp()
    {
        _session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, V1,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        _session.MarkHeld();
        _attendance = new InMemoryAttendanceRepository();
        _record = new RecordAttendanceHandler(
            new InMemoryEligibilityProvider().Add(OfferingId, V1, Ana), _attendance);
    }

    [Test]
    public async Task RepeatedEntryForAna_ChangesSameRecord()
    {
        await _record.HandleAsync(_session, Ana, AttendanceState.Odsutan, Guid.NewGuid());
        var before = (await _attendance.GetAsync(_session.Id))!.Records.Single(r => r.StudentId == Ana);

        await _record.HandleAsync(_session, Ana, AttendanceState.Prisutan, Guid.NewGuid());
        var stored = await _attendance.GetAsync(_session.Id);
        var after = stored!.Records.Where(r => r.StudentId == Ana).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(after, Has.Length.EqualTo(1));
            Assert.That(after[0].Id, Is.EqualTo(before.Id));
            Assert.That(after[0].State, Is.EqualTo(AttendanceState.Prisutan));
        });
    }
}
