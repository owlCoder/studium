using Domain.Materials;
using NUnit.Framework;

namespace Tests.Domain;

/// <summary>
/// ST-009, US-09 (tačka 1): promena stanja materijala iz objavljen u povučen.
/// </summary>
[TestFixture]
public sealed class MaterialWithdrawTests
{
    private static Material Draft() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", "Uvod");

    private static Material Published()
    {
        var material = Draft();
        material.Publish();
        return material;
    }

    // Pozitivno ponašanje

    [Test]
    public void Withdraw_ChangesPublishedToWithdrawn()
    {
        var material = Published();

        material.Withdraw();

        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
    }

    [Test]
    public void Withdraw_DoesNotDeleteOrChangeOtherFields()
    {
        var material = Draft();
        var modifiedAt = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        material.UpdateDetails("Predavanje 2", "Novi opis", modifiedAt);
        material.Publish();

        material.Withdraw();

        Assert.Multiple(() =>
        {
            Assert.That(material.Name, Is.EqualTo("Predavanje 2"));
            Assert.That(material.Description, Is.EqualTo("Novi opis"));
            Assert.That(material.LastModifiedAt, Is.EqualTo(modifiedAt));
        });
    }

    // Negativno ponašanje

    [Test]
    public void Withdraw_Draft_IsRejectedAndStateStaysDraft()
    {
        var material = Draft();

        Assert.That(() => material.Withdraw(), Throws.TypeOf<InvalidOperationException>());
        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
    }

    // Granično ponašanje

    [Test]
    public void Withdraw_AlreadyWithdrawn_IsRejectedAndStateStaysWithdrawn()
    {
        var material = Published();
        material.Withdraw();

        Assert.That(() => material.Withdraw(), Throws.TypeOf<InvalidOperationException>());
        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
    }
}
