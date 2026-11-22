using Domain.Activities;
using Infrastructure.Dashboard;
using NUnit.Framework;
using Tests.Application.Activities;

namespace Tests.Acceptance;

[TestFixture]
[Category("Acceptance")]
public sealed class St007AcceptanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task StudentIzDvaPredmetaSaJednimOtvorenimZadatkom_VidiObaPredmetaIVezuKaZadatku()
    {
        var scenario = new St007Scenario();
        var ers = scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        var oib = scenario.AddEnrolledOffering("OIB", "Osnove informacione bezbednosti");
        var taskId = Guid.NewGuid();
        scenario.Activities.Add(new Activity(taskId, ers, SupportedFeatures.Assignment,
            isPublished: true, deadline: Now.AddDays(7)));

        var overview = await scenario.CreateHandler(new FixedTimeProvider(Now))
            .HandleAsync(St007Scenario.StudentId);

        var ersCard = overview.Enrolled.Single(x => x.OfferingId == ers);
        var oibCard = overview.Enrolled.Single(x => x.OfferingId == oib);

        Assert.Multiple(() =>
        {
            Assert.That(overview.Enrolled.Select(x => x.CourseName), Is.EquivalentTo(new[]
            {
                "Elementi razvoja softvera",
                "Osnove informacione bezbednosti"
            }));
            Assert.That(ersCard.OpenActivities, Is.EqualTo(1));
            Assert.That(oibCard.OpenActivities, Is.EqualTo(0));

            // Veza ka zadacima predmeta u kom je otvoren zadatak (US-7.4 + US-7.6).
            Assert.That(ersCard.Links.Select(l => l.Href), Does.Contain($"/offerings/{ers}/assignments"));

            // Registar (US-7.6) daje vezu baš ka tom zadatku; prikaz veze po zadatku dodaje US-7.10.
            Assert.That(SupportedFeatures.CreateRegistry().TryBuildLink(SupportedFeatures.Assignment, ers, taskId),
                Is.EqualTo($"/offerings/{ers}/assignments/{taskId}"));
        });
    }

    [Test]
    public async Task PromenaRoka_OdmahSeVidiNaPocetnojStrani()
    {
        var scenario = new St007Scenario();
        var ers = scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        var taskId = Guid.NewGuid();
        scenario.Activities.Add(new Activity(taskId, ers, SupportedFeatures.Assignment, true, Now.AddDays(2)));
        var handler = scenario.CreateHandler(new FixedTimeProvider(Now));

        var before = await handler.HandleAsync(St007Scenario.StudentId);

        scenario.Activities.ChangeDeadline(taskId, Now.AddHours(-1));   // rok pomeren u prošlost
        var afterShortening = await handler.HandleAsync(St007Scenario.StudentId);

        scenario.Activities.ChangeDeadline(taskId, Now.AddDays(5));     // rok ponovo produžen
        var afterExtension = await handler.HandleAsync(St007Scenario.StudentId);

        Assert.Multiple(() =>
        {
            Assert.That(before.Enrolled.Single().OpenActivities, Is.EqualTo(1));
            Assert.That(afterShortening.Enrolled.Single().OpenActivities, Is.EqualTo(0));
            Assert.That(afterExtension.Enrolled.Single().OpenActivities, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task NemaKesa_SvakiZahtevPonovoCitaIzvorneEvidencije()
    {
        var scenario = new St007Scenario();
        var ers = scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        var handler = scenario.CreateHandler(new FixedTimeProvider(Now));

        var empty = await handler.HandleAsync(St007Scenario.StudentId);
        scenario.Activities.Add(new Activity(Guid.NewGuid(), ers, SupportedFeatures.Assignment, true, Now.AddDays(1)));
        var withTask = await handler.HandleAsync(St007Scenario.StudentId);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Enrolled.Single().OpenActivities, Is.EqualTo(0));
            Assert.That(withTask.Enrolled.Single().OpenActivities, Is.EqualTo(1));
            Assert.That(scenario.Activities.ActivityReads, Is.EqualTo(2), "Izvor mora biti pročitan pri svakom zahtevu.");
        });
    }

    [Test]
    public async Task PredajaZadatka_OdmahUklanjaOtvorenuAktivnost()
    {
        var scenario = new St007Scenario();
        var ers = scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        var taskId = Guid.NewGuid();
        scenario.Activities.Add(new Activity(taskId, ers, SupportedFeatures.Assignment, true, Now.AddDays(3)));
        var handler = scenario.CreateHandler(new FixedTimeProvider(Now));

        var before = await handler.HandleAsync(St007Scenario.StudentId);
        scenario.Activities.AddSubmission(new ActivitySubmission(Guid.NewGuid(), taskId, St007Scenario.StudentId));
        var after = await handler.HandleAsync(St007Scenario.StudentId);

        Assert.Multiple(() =>
        {
            Assert.That(before.Enrolled.Single().OpenActivities, Is.EqualTo(1));
            Assert.That(after.Enrolled.Single().OpenActivities, Is.EqualTo(0));
        });
    }
}
