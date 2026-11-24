using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;
using Domain.Dashboard;
using NUnit.Framework;

namespace Tests.Domain.Activities;

[TestFixture]
public sealed class OpenActivityRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid Offering = Guid.NewGuid();

    // Registar kao kod Milene (US-7.6): "assignment" je podržan, "quiz" nije.
    private static readonly OpenActivityRule Rule = new(new FeatureRegistry(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    }));

    private static Activity NewActivity(
        bool published = true,
        DateTimeOffset? deadline = null,
        string type = "assignment") =>
        new(Guid.NewGuid(), Offering, type, published, deadline ?? Now.AddDays(3));

    [Test]
    public void ObjavljenaSaRokomUBuducnostiBezPredaje_JeOtvorena()
    {
        var activity = NewActivity();

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.True);
    }

    [Test]
    public void NeobjavljenaAktivnost_NijeOtvorena()
    {
        var activity = NewActivity(published: false);

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.False);
    }

    [Test]
    public void RokProsao_NijeOtvorena()
    {
        var activity = NewActivity(deadline: Now.AddMinutes(-1));

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.False);
    }

    [Test]
    public void TacnoUTrenutkuRoka_NijeOtvorena()
    {
        var activity = NewActivity(deadline: Now);

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.False);
    }

    [Test]
    public void SaVazecomPredajom_NijeOtvorena()
    {
        var activity = NewActivity();
        var submissions = new[] { new ActivitySubmission(Guid.NewGuid(), activity.Id, Student) };

        Assert.That(Rule.IsOpen(activity, Student, submissions, Now), Is.False);
    }

    [Test]
    public void PovucenaPredaja_NijeVazeca_AktivnostOstajeOtvorena()
    {
        var activity = NewActivity();
        var submissions = new[]
        {
            new ActivitySubmission(Guid.NewGuid(), activity.Id, Student, ActivitySubmissionState.Withdrawn)
        };

        Assert.That(Rule.IsOpen(activity, Student, submissions, Now), Is.True);
    }

    [Test]
    public void PredajaDrugogStudenta_NeZatvaraAktivnost()
    {
        var activity = NewActivity();
        var submissions = new[] { new ActivitySubmission(Guid.NewGuid(), activity.Id, Guid.NewGuid()) };

        Assert.That(Rule.IsOpen(activity, Student, submissions, Now), Is.True);
    }

    [TestCase("quiz")]
    [TestCase("team-assignment")]
    public void NepodrzanaVrsta_SeNeBroji(string type)
    {
        var activity = NewActivity(type: type);

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.False);
    }

    [Test]
    public void CountOpen_IstaAktivnostDvaput_BrojiSeJednom()
    {
        var activity = NewActivity();

        var count = Rule.CountOpen(
            new[] { activity, activity }, Student, Array.Empty<ActivitySubmission>(), Now);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void VrstaSeUporedjujeBezObziraNaVelikaSlova()
    {
        var activity = NewActivity(type: "  Assignment ");

        Assert.That(Rule.IsOpen(activity, Student, Array.Empty<ActivitySubmission>(), Now), Is.True);
    }

    [Test]
    public void Aktivnost_OdbijaPrazanIdentifikatorIVrstu()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new Activity(Guid.Empty, Offering, "assignment", true, Now));
            Assert.Throws<ArgumentException>(() => new Activity(Guid.NewGuid(), Offering, " ", true, Now));
        });
    }
}
