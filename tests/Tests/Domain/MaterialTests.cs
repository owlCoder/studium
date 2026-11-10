using NUnit.Framework;
using Domain.Materials;

namespace Tests.Domain;

[TestFixture]
public sealed class MaterialTests
{
    [Test]
    public void Constructor_CreatesDraftWithNormalizedFields()
    {
        var id = Guid.NewGuid();
        var offeringId = Guid.NewGuid();

        var material = new Material(id, offeringId, "  Predavanje 1 ", "  Uvod u predmet  ");

        Assert.Multiple(() =>
        {
            Assert.That(material.Id, Is.EqualTo(id));
            Assert.That(material.CourseOfferingId, Is.EqualTo(offeringId));
            Assert.That(material.Name, Is.EqualTo("Predavanje 1"));
            Assert.That(material.Description, Is.EqualTo("Uvod u predmet"));
            Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_AllowsMissingDescription(string? description)
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", description);

        Assert.That(material.Description, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_RejectsBlankName(string? name)
    {
        Assert.That(
            () => new Material(Guid.NewGuid(), Guid.NewGuid(), name!, "Opis"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("name"));
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.That(
            () => new Material(Guid.Empty, Guid.NewGuid(), "Predavanje 1", null),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("id"));
    }

    [Test]
    public void Constructor_RejectsEmptyCourseOffering()
    {
        Assert.That(
            () => new Material(Guid.NewGuid(), Guid.Empty, "Predavanje 1", null),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("courseOfferingId"));
    }

    [Test]
    public void Constructor_AcceptsSingleCharacterName()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "A", null);

        Assert.That(material.Name, Is.EqualTo("A"));
    }

    // ST-009, US-02: izmena naziva i opisa

    [Test]
    public void UpdateDetails_ChangesAndTrimsNameAndDescription()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");

        material.UpdateDetails("  Novo ime ", "  Novi opis ");

        Assert.Multiple(() =>
        {
            Assert.That(material.Name, Is.EqualTo("Novo ime"));
            Assert.That(material.Description, Is.EqualTo("Novi opis"));
        });
    }

    [Test]
    public void UpdateDetails_DoesNotChangeIdentityOfferingOrPublicationState()
    {
        var id = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var material = new Material(id, offeringId, "Staro", null);

        material.UpdateDetails("Novo", "Opis");

        Assert.Multiple(() =>
        {
            Assert.That(material.Id, Is.EqualTo(id));
            Assert.That(material.CourseOfferingId, Is.EqualTo(offeringId));
            Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDetails_RejectsBlankNameAndKeepsPreviousValues(string? name)
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");

        Assert.That(
            () => material.UpdateDetails(name!, "Novi opis"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("name"));

        Assert.Multiple(() =>
        {
            Assert.That(material.Name, Is.EqualTo("Staro"));
            Assert.That(material.Description, Is.EqualTo("Stari opis"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDetails_AllowsClearingDescription(string? description)
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Naziv", "Opis");

        material.UpdateDetails("Naziv", description);

        Assert.That(material.Description, Is.Empty);
    }

    [Test]
    public void UpdateDetails_AcceptsSingleCharacterName()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Naziv", null);

        material.UpdateDetails("B", null);

        Assert.That(material.Name, Is.EqualTo("B"));
    }
}
