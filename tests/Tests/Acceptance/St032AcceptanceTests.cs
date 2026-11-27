using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Acceptance;

/// <summary>
/// Testovi prihvatanja za ST-032, po primeru iz specifikacije:
/// "Za održan termin ima dvadeset podobnih studenata. Prisustvo je uneto za
/// osamnaest. Za preostala dva studenta stanje je 'Neevidentirano', a ne 'Odsutan'."
/// </summary>
[TestFixture]
public sealed class St032AcceptanceTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();

    private TeachingSession _session = null!;
    private Guid[] _students = null!;
    private InMemoryEligibilityProvider _eligibility = null!;
    private InMemoryAttendanceRepository _attendance = null!;
    private RecordAttendanceHandler _record = null!;
    private ListEligibleStudentsHandler _list = null!;

    [SetUp]
    public void SetUp()
    {
        _session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, V1,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        _session.MarkHeld();

        _students = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        _eligibility = new InMemoryEligibilityProvider();
        foreach (var student in _students)
            _eligibility.Add(OfferingId, V1, student);

        _attendance = new InMemoryAttendanceRepository();
        _record = new RecordAttendanceHandler(_eligibility, _attendance);
        _list = new ListEligibleStudentsHandler(_eligibility, _attendance);
    }

    private async Task RecordFirstEighteen()
    {
        var actor = Guid.NewGuid();
        for (var i = 0; i < 18; i++)
        {
            // Mešavina prisutnih i odsutnih, kao na stvarnom času.
            var state = i % 3 == 0 ? AttendanceState.Odsutan : AttendanceState.Prisutan;
            await _record.HandleAsync(_session, _students[i], state, actor);
        }
    }

    [Test]
    public async Task TwentyEligible_EighteenRecorded_TwoAreNeevidentirano()
    {
        await RecordFirstEighteen();

        var result = await _list.HandleAsync(_session);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(20));
            Assert.That(result.Count(s => s.State != AttendanceState.Neevidentirano), Is.EqualTo(18));
            Assert.That(result.Count(s => s.State == AttendanceState.Neevidentirano), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task TwoUnrecordedStudents_AreNotOdsutan()
    {
        await RecordFirstEighteen();

        var result = await _list.HandleAsync(_session);
        var unrecorded = result.Where(s => s.StudentId == _students[18] || s.StudentId == _students[19]);

        Assert.That(unrecorded.Select(s => s.State),
            Is.All.EqualTo(AttendanceState.Neevidentirano));
    }

    [Test]
    public async Task OnlyEighteenRecordsExist_UnrecordedStudentsHaveNoRecord()
    {
        await RecordFirstEighteen();

        var stored = await _attendance.GetAsync(_session.Id);

        Assert.That(stored!.Records, Has.Count.EqualTo(18));
    }
}
