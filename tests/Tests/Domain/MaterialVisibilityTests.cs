using Domain.Materials;
using NUnit.Framework;

namespace Tests.Domain;

/// <summary>
/// ST-009, „Zaštita pristupa materijalu i prilozima”, tačka 2:
/// student vidi samo objavljen materijal; nacrt i povučen materijal nisu vidljivi.
/// </summary>
[TestFixture]
public sealed class MaterialVisibilityTests
{
    [Test]
    public void IsVisibleToStudents_Published_IsVisible()
    {
        Assert.That(MaterialPublicationState.Published.IsVisibleToStudents(), Is.True);
    }

    [TestCase(MaterialPublicationState.Draft)]
    [TestCase(MaterialPublicationState.Withdrawn)]
    public void IsVisibleToStudents_DraftAndWithdrawn_AreHidden(MaterialPublicationState state)
    {
        Assert.That(state.IsVisibleToStudents(), Is.False);
    }

    [Test]
    public void IsVisibleToStudents_OnlyPublishedOfAllStatesIsVisible()
    {
        // Ako se doda novo stanje, ono je podrazumevano nevidljivo za studente.
        var visible = Enum.GetValues<MaterialPublicationState>()
            .Where(s => s.IsVisibleToStudents())
            .ToArray();

        Assert.That(visible, Is.EqualTo(new[] { MaterialPublicationState.Published }));
    }

    [Test]
    public void NewMaterial_IsNotVisibleToStudents()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);

        Assert.That(material.PublicationState.IsVisibleToStudents(), Is.False);
    }

    [Test]
    public void PublishedMaterial_IsVisibleToStudents()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        material.Publish();

        Assert.That(material.PublicationState.IsVisibleToStudents(), Is.True);
    }
}
