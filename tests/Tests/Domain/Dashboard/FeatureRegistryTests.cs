using Domain.Dashboard;
using NUnit.Framework;

namespace Tests.Domain.Dashboard;

[TestFixture]
public sealed class FeatureRegistryTests
{
    private static FeatureRegistry CreateRegistry() => new(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments, "/offerings/{offeringId}/assignments/{itemId}"),
        new SupportedActivityType("announcement", FunctionalArea.Announcements, "/offerings/{offeringId}/announcements/{itemId}")
    });

    [Test]
    public void IsActivitySupported_VracaTrueZaPodrzanuVrstu()
    {
        Assert.That(CreateRegistry().IsActivitySupported("assignment"), Is.True);
    }

    [Test]
    public void IsActivitySupported_IgnoriseVelikaSlovaIRazmake()
    {
        Assert.That(CreateRegistry().IsActivitySupported("  ASSIGNMENT "), Is.True);
    }

    [TestCase("quiz")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void IsActivitySupported_VracaFalseZaNepodrzanuIliPraznuVrstu(string? activityType)
    {
        Assert.That(CreateRegistry().IsActivitySupported(activityType), Is.False);
    }

    [Test]
    public void IsAreaAvailable_RazlikujeDostupneINedostupneOblasti()
    {
        var registry = CreateRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsAreaAvailable(FunctionalArea.Assignments), Is.True);
            Assert.That(registry.IsAreaAvailable(FunctionalArea.Materials), Is.False);
        });
    }

    [Test]
    public void TryBuildLink_PravaVezuZaPodrzanuVrstu()
    {
        var offeringId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var link = CreateRegistry().TryBuildLink("assignment", offeringId, itemId);

        Assert.That(link, Is.EqualTo($"/offerings/{offeringId}/assignments/{itemId}"));
    }

    [Test]
    public void TryBuildLink_NepodrzanaVrstaNemaVezu()
    {
        Assert.That(CreateRegistry().TryBuildLink("quiz", Guid.NewGuid(), Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void TryBuildLink_PrazanIdentifikatorNemaVezu()
    {
        Assert.That(CreateRegistry().TryBuildLink("assignment", Guid.Empty, Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void Constructor_OdbijaDuplikatVrste()
    {
        Assert.That(() => new FeatureRegistry(new[]
        {
            new SupportedActivityType("assignment", FunctionalArea.Assignments, "/a/{itemId}"),
            new SupportedActivityType("Assignment", FunctionalArea.Assignments, "/b/{itemId}")
        }), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void SupportedActivityType_OdbijaPraznuOznaku()
    {
        Assert.That(() => new SupportedActivityType(" ", FunctionalArea.Assignments, "/a"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void SupportedActivityType_OdbijaApsolutnuIliPraznuVezu()
    {
        Assert.That(() => new SupportedActivityType("assignment", FunctionalArea.Assignments, "https://x.y"),
            Throws.TypeOf<ArgumentException>());
    }
}
