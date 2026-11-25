using Domain.AttendanceOverview;
using NUnit.Framework;

namespace Tests.Domain;

/// <summary>
/// Testovi pravila računanja iz ST-034 (Pregled prisustva studenta).
/// Oznake P/G/N odgovaraju kriterijumima prihvatanja iz backloga.
/// </summary>
[TestFixture]
public sealed class AttendanceSummaryTests
{
    // ── Pozitivni scenariji ─────────────────────────────────────────────────

    [Test]
    public void Percentage_SpecificationExample_IsEightyPercent()
    {
        // Primer iz specifikacije: 4 Prisutan, 1 Odsutan, 2 Neevidentirano → 4/5 = 80%.
        var summary = new AttendanceSummary(present: 4, absent: 1, unrecorded: 2);

        Assert.Multiple(() =>
        {
            Assert.That(summary.Percentage, Is.EqualTo(80m));
            Assert.That(summary.PercentageText(), Is.EqualTo("80%"));
            Assert.That(summary.HeldSessions, Is.EqualTo(7));
            Assert.That(summary.Unrecorded, Is.EqualTo(2));
        });
    }

    [Test]
    public void Percentage_AllPresent_IsHundredPercent()
    {
        var summary = new AttendanceSummary(present: 5, absent: 0, unrecorded: 0);

        Assert.That(summary.Percentage, Is.EqualTo(100m));
    }

    // ── Granični scenariji ──────────────────────────────────────────────────

    [Test]
    public void Percentage_UnrecordedIsNotInDenominator()
    {
        // Da se Neevidentirano računa kao odsustvo, rezultat bi bio 4/7 = 57,14%.
        var summary = new AttendanceSummary(present: 4, absent: 1, unrecorded: 2);

        Assert.That(summary.Percentage, Is.Not.EqualTo(57.14m));
    }

    [Test]
    public void Percentage_TwoOfThree_IsRoundedToTwoDecimals()
    {
        var summary = new AttendanceSummary(present: 2, absent: 1, unrecorded: 0);

        Assert.Multiple(() =>
        {
            Assert.That(summary.Percentage, Is.EqualTo(66.67m));
            Assert.That(summary.PercentageText(), Is.EqualTo("66,67%"));
        });
    }

    [Test]
    public void Percentage_ExactHalfAtThirdDecimal_RoundsUp()
    {
        // 1 / (1 + 31) = 1/32 = 3,125% (tačno na polovini) → zaokružuje se naviše na 3,13%.
        var summary = new AttendanceSummary(present: 1, absent: 31, unrecorded: 0);

        Assert.That(summary.Percentage, Is.EqualTo(3.13m));
    }

    [Test]
    public void Percentage_NoPresentOrAbsent_ShowsNoRecords()
    {
        // Samo Neevidentirano: nema osnove za procenat, ni 0% ni deljenje nulom.
        var summary = new AttendanceSummary(present: 0, absent: 0, unrecorded: 3);

        Assert.Multiple(() =>
        {
            Assert.That(summary.HasRecords, Is.False);
            Assert.That(summary.Percentage, Is.Null);
            Assert.That(summary.PercentageText(), Is.EqualTo(AttendanceSummary.NoRecordsText));
            Assert.That(summary.Unrecorded, Is.EqualTo(3));
        });
    }

    [Test]
    public void Percentage_NoSessionsAtAll_ShowsNoRecords()
    {
        var summary = new AttendanceSummary(present: 0, absent: 0, unrecorded: 0);

        Assert.Multiple(() =>
        {
            Assert.That(summary.HeldSessions, Is.EqualTo(0));
            Assert.That(summary.PercentageText(), Is.EqualTo("Nema evidencije"));
        });
    }

    [Test]
    public void Percentage_AllAbsent_IsZeroPercent_NotNoRecords()
    {
        // 0% je stvaran rezultat kada postoje zapisi Odsutan; razlikuje se od „Nema evidencije”.
        var summary = new AttendanceSummary(present: 0, absent: 4, unrecorded: 0);

        Assert.Multiple(() =>
        {
            Assert.That(summary.HasRecords, Is.True);
            Assert.That(summary.Percentage, Is.EqualTo(0m));
            Assert.That(summary.PercentageText(), Is.EqualTo("0%"));
        });
    }

    // ── Negativni scenariji ─────────────────────────────────────────────────

    [TestCase(-1, 0, 0)]
    [TestCase(0, -1, 0)]
    [TestCase(0, 0, -1)]
    public void Constructor_RejectsNegativeCounts(int present, int absent, int unrecorded)
    {
        Assert.That(
            () => new AttendanceSummary(present, absent, unrecorded),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
