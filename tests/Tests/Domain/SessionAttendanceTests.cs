using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain;

[TestFixture]
public sealed class SessionAttendanceTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Test]
    public void StateOf_WithoutRecord_IsNeevidentirano()
    {
        var attendance = new SessionAttendance(SessionId);

        Assert.That(
            attendance.StateOf(StudentId),
            Is.EqualTo(AttendanceState.Neevidentirano));
    }

    [Test]
    public void Record_CreatesSingleRecordPerStudent()
    {
        var attendance = new SessionAttendance(SessionId);

        attendance.Record(StudentId, AttendanceState.Prisutan, ActorId);

        Assert.Multiple(() =>
        {
            Assert.That(attendance.Records, Has.Count.EqualTo(1));
            Assert.That(attendance.StateOf(StudentId), Is.EqualTo(AttendanceState.Prisutan));
        });
    }

    [Test]
    public void Record_SecondEntry_UpdatesSameRecordAndSavesActor()
    {
        var attendance = new SessionAttendance(SessionId);
        var first = attendance.Record(StudentId, AttendanceState.Prisutan, ActorId);
        var secondActor = Guid.NewGuid();

        var second = attendance.Record(StudentId, AttendanceState.Odsutan, secondActor);

        Assert.Multiple(() =>
        {
            Assert.That(attendance.Records, Has.Count.EqualTo(1));
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(second.State, Is.EqualTo(AttendanceState.Odsutan));
            Assert.That(second.ActorId, Is.EqualTo(secondActor));
        });
    }

    [Test]
    public void Record_RejectsNeevidentirano()
    {
        var attendance = new SessionAttendance(SessionId);

        Assert.That(
            () => attendance.Record(StudentId, AttendanceState.Neevidentirano, ActorId),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Record_TwoStudents_CreatesTwoRecords()
    {
        var attendance = new SessionAttendance(SessionId);
        var otherStudent = Guid.NewGuid();

        attendance.Record(StudentId, AttendanceState.Prisutan, ActorId);
        attendance.Record(otherStudent, AttendanceState.Odsutan, ActorId);

        Assert.Multiple(() =>
        {
            Assert.That(attendance.Records, Has.Count.EqualTo(2));
            Assert.That(attendance.StateOf(StudentId), Is.EqualTo(AttendanceState.Prisutan));
            Assert.That(attendance.StateOf(otherStudent), Is.EqualTo(AttendanceState.Odsutan));
        });
    }
}
