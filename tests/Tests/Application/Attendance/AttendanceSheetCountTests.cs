using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Brojanje unetih i neevidentiranih na spisku za prisustvo (ST-032).
/// Primer iz specifikacije: od 20 podobnih uneto je 18.
/// </summary>
[TestFixture]
public sealed class AttendanceSheetCountTests
{
    private static AttendanceSheet SheetWith(int present, int absent, int unrecorded)
    {
        var students = Enumerable.Repeat(AttendanceState.Prisutan, present)
            .Concat(Enumerable.Repeat(AttendanceState.Odsutan, absent))
            .Concat(Enumerable.Repeat(AttendanceState.Neevidentirano, unrecorded))
            .Select(state => new EligibleStudent(Guid.NewGuid(), state))
            .ToArray();

        return new AttendanceSheet(Guid.NewGuid(), students);
    }

    [Test]
    public void TwentyEligible_EighteenEntered_TwoAreUnrecorded()
    {
        var sheet = SheetWith(present: 15, absent: 3, unrecorded: 2);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.EligibleCount, Is.EqualTo(20));
            Assert.That(sheet.EnteredCount, Is.EqualTo(18));
            Assert.That(sheet.UnrecordedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void AbsentStudents_CountAsEntered_NotAsUnrecorded()
    {
        var sheet = SheetWith(present: 0, absent: 4, unrecorded: 1);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.EnteredCount, Is.EqualTo(4));
            Assert.That(sheet.UnrecordedCount, Is.EqualTo(1));
        });
    }
}
