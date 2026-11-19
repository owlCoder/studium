using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain;

[TestFixture]
public sealed class AttendanceNoteTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Test]
    public void Record_SecondEntry_ChangesNoteWithoutNewRow()
    {
        var attendance = new SessionAttendance(SessionId);
        var first = attendance.Record(
            StudentId, AttendanceState.Prisutan, ActorId, "Prva napomena");

        var second = attendance.Record(
            StudentId, AttendanceState.Odsutan, ActorId, "Druga napomena");

        Assert.Multiple(() =>
        {
            Assert.That(attendance.Records, Has.Count.EqualTo(1));
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(second.Note, Is.EqualTo("Druga napomena"));
        });
    }

    [Test]
    public void Record_WithoutNote_StoresEmptyNote()
    {
        var attendance = new SessionAttendance(SessionId);

        var record = attendance.Record(StudentId, AttendanceState.Prisutan, ActorId);

        Assert.That(record.Note, Is.EqualTo(string.Empty));
    }
}
