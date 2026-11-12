using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain;

[TestFixture]
public sealed class AttendanceRecordTests
{
    [Test]
    public void Constructor_RejectsNeevidentirano()
    {
        Assert.That(
            () => new AttendanceRecord(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                AttendanceState.Neevidentirano, Guid.NewGuid()),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Record_ChangesStateAndActor()
    {
        var record = new AttendanceRecord(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            AttendanceState.Prisutan, Guid.NewGuid());
        var newActor = Guid.NewGuid();

        record.Record(AttendanceState.Odsutan, newActor);

        Assert.Multiple(() =>
        {
            Assert.That(record.State, Is.EqualTo(AttendanceState.Odsutan));
            Assert.That(record.ActorId, Is.EqualTo(newActor));
        });
    }

    [Test]
    public void Record_RejectsNeevidentirano()
    {
        var record = new AttendanceRecord(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            AttendanceState.Prisutan, Guid.NewGuid());

        Assert.That(
            () => record.Record(AttendanceState.Neevidentirano, Guid.NewGuid()),
            Throws.TypeOf<ArgumentException>());
    }
}
