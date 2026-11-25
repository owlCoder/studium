using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;
using Domain.Dashboard;
using NUnit.Framework;

namespace Tests.Domain.Activities;

/// <summary>US-7.9: pojedinačni rok studenta (ST-015) u pravilu otvorene aktivnosti.</summary>
[TestFixture]
public sealed class IndividualDeadlineRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid Offering = Guid.NewGuid();

    private static readonly OpenActivityRule Rule = new(new FeatureRegistry(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    }));

    private static Activity ActivityWithDeadline(DateTimeOffset deadline) =>
        new(Guid.NewGuid(), Offering, "assignment", isPublished: true, deadline);

    [Test]
    public void ProduzenRok_VracaZadatakUOtvorene()
    {
        var activity = ActivityWithDeadline(Now.AddHours(-1));          // zajednički rok prošao
        var extended = new[] { new IndividualDeadline(activity.Id, Student, Now.AddDays(2)) };

        Assert.Multiple(() =>
        {
            Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.False);
            Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), extended, Now), Is.True);
        });
    }

    [Test]
    public void PojedinacniRokImaPrednost_IKadaJeRaniji()
    {
        var activity = ActivityWithDeadline(Now.AddDays(5));
        var earlier = new[] { new IndividualDeadline(activity.Id, Student, Now.AddHours(-1)) };

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), earlier, Now), Is.False);
    }

    [Test]
    public void ProduzenjeDrugomStudentu_NeVazi()
    {
        var activity = ActivityWithDeadline(Now.AddHours(-1));
        var forOther = new[] { new IndividualDeadline(activity.Id, Guid.NewGuid(), Now.AddDays(2)) };

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), forOther, Now), Is.False);
    }

    [Test]
    public void ProduzenjeZaDruguAktivnost_NeVazi()
    {
        var activity = ActivityWithDeadline(Now.AddHours(-1));
        var forOtherActivity = new[] { new IndividualDeadline(Guid.NewGuid(), Student, Now.AddDays(2)) };

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), forOtherActivity, Now), Is.False);
    }

    [Test]
    public void ProduzenRokSaVazecomPredajom_NijeOtvoren()
    {
        var activity = ActivityWithDeadline(Now.AddHours(-1));
        var extended = new[] { new IndividualDeadline(activity.Id, Student, Now.AddDays(2)) };
        var submitted = new[] { new ActivitySubmission(Guid.NewGuid(), activity.Id, Student) };

        Assert.That(Rule.IsOpen(activity, Student, submitted, extended, Now), Is.False);
    }

    [Test]
    public void EffectiveDeadline_BezProduzenjaJeZajednickiRok()
    {
        var activity = ActivityWithDeadline(Now.AddDays(1));

        Assert.That(OpenActivityRule.EffectiveDeadline(activity, Student, Array.Empty<IndividualDeadline>()),
            Is.EqualTo(activity.Deadline));
    }

    [Test]
    public void IndividualDeadline_OdbijaPrazneIdentifikatore()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new IndividualDeadline(Guid.Empty, Student, Now));
            Assert.Throws<ArgumentException>(() => new IndividualDeadline(Guid.NewGuid(), Guid.Empty, Now));
        });
    }
}
