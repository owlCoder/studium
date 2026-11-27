using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Granični slučajevi brojanja na spisku za prisustvo (ST-032).
/// </summary>
[TestFixture]
public sealed class AttendanceSheetEdgeTests
{
    [Test]
    public void EmptySheet_HasZeroEverywhere()
    {
        var sheet = new AttendanceSheet(Guid.NewGuid(), Array.Empty<EligibleStudent>());

        Assert.Multiple(() =>
        {
            Assert.That(sheet.EligibleCount, Is.Zero);
            Assert.That(sheet.EnteredCount, Is.Zero);
            Assert.That(sheet.UnrecordedCount, Is.Zero);
        });
    }

    [Test]
    public void NobodyEntered_AllAreUnrecorded()
    {
        var students = Enumerable.Range(0, 3)
            .Select(_ => new EligibleStudent(Guid.NewGuid(), AttendanceState.Neevidentirano))
            .ToArray();

        var sheet = new AttendanceSheet(Guid.NewGuid(), students);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.EnteredCount, Is.Zero);
            Assert.That(sheet.UnrecordedCount, Is.EqualTo(3));
        });
    }
}
