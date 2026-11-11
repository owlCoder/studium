using Application.Dashboard;
using Domain.Dashboard;
using NUnit.Framework;

namespace Tests.Application.Dashboard;

[TestFixture]
public sealed class FilterSupportedActivitiesHandlerTests
{
    private static FilterSupportedActivitiesHandler CreateHandler() => new(new FeatureRegistry(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments, "/offerings/{offeringId}/assignments/{itemId}"),
        new SupportedActivityType("announcement", FunctionalArea.Announcements, "/offerings/{offeringId}/announcements/{itemId}")
    }));

    [Test]
    public void Handle_NepodrzanaVrstaAktivnostiSeNeBrojiINemaVezu()
    {
        var offeringId = Guid.NewGuid();
        var activities = new[]
        {
            new DashboardActivity("assignment", offeringId, Guid.NewGuid(), "Domaći 1"),
            new DashboardActivity("quiz", offeringId, Guid.NewGuid(), "Kviz 1"),
            new DashboardActivity("announcement", offeringId, Guid.NewGuid(), "Pomeren termin")
        };

        var result = CreateHandler().Handle(activities);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.CountsByType.ContainsKey("quiz"), Is.False);
            Assert.That(result.Items.Select(x => x.Title), Does.Not.Contain("Kviz 1"));
            Assert.That(result.Items.Any(x => x.Link.Contains("quiz")), Is.False);
            Assert.That(result.Items.All(x => !string.IsNullOrWhiteSpace(x.Link)), Is.True);
        });
    }

    [Test]
    public void Handle_BrojiPodrzaneVrstePoTipu()
    {
        var offeringId = Guid.NewGuid();
        var activities = new[]
        {
            new DashboardActivity("assignment", offeringId, Guid.NewGuid(), "Domaći 1"),
            new DashboardActivity("ASSIGNMENT", offeringId, Guid.NewGuid(), "Domaći 2"),
            new DashboardActivity("announcement", offeringId, Guid.NewGuid(), "Obaveštenje")
        };

        var result = CreateHandler().Handle(activities);

        Assert.Multiple(() =>
        {
            Assert.That(result.CountsByType["assignment"], Is.EqualTo(2));
            Assert.That(result.CountsByType["announcement"], Is.EqualTo(1));
        });
    }

    [Test]
    public void Handle_SveNepodrzaneDajuPrazanPregled()
    {
        var activities = new[]
        {
            new DashboardActivity("quiz", Guid.NewGuid(), Guid.NewGuid(), "Kviz"),
            new DashboardActivity(null, Guid.NewGuid(), Guid.NewGuid(), "Bez vrste")
        };

        var result = CreateHandler().Handle(activities);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.Zero);
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.CountsByType, Is.Empty);
        });
    }

    [Test]
    public void Handle_PraznaListaDajePrazanPregled()
    {
        var result = CreateHandler().Handle(Array.Empty<DashboardActivity>());

        Assert.That(result.TotalCount, Is.Zero);
    }

    [Test]
    public void Handle_OdbijaNullListu()
    {
        Assert.That(() => CreateHandler().Handle(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
