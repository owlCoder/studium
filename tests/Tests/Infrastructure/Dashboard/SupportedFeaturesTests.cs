using Domain.Dashboard;
using Infrastructure.Dashboard;
using NUnit.Framework;

namespace Tests.Infrastructure.Dashboard;

[TestFixture]
public sealed class SupportedFeaturesTests
{
    [Test]
    public void CreateRegistry_SadrziSvePodrzaneVrsteIOblasti()
    {
        var registry = SupportedFeatures.CreateRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsActivitySupported(SupportedFeatures.Announcement), Is.True);
            Assert.That(registry.IsActivitySupported(SupportedFeatures.Material), Is.True);
            Assert.That(registry.IsActivitySupported(SupportedFeatures.Assignment), Is.True);
            Assert.That(registry.AvailableAreas, Is.EquivalentTo(Enum.GetValues<FunctionalArea>()));
        });
    }

    [Test]
    public void CreateRegistry_NePodrzavaVrstuKojaNijeNaListi()
    {
        Assert.That(SupportedFeatures.CreateRegistry().IsActivitySupported("quiz"), Is.False);
    }
}
