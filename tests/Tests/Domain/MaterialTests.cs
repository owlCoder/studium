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

    // ST-009, US-02: izmena naziva i opisa i vreme poslednje izmene

    private static readonly DateTimeOffset EditTime =
        new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void UpdateDetails_ChangesAndTrimsNameAndDescription()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");

        material.UpdateDetails("  Novo ime ", "  Novi opis ", EditTime);

        Assert.Multiple(() =>
        {
            Assert.That(material.Name, Is.EqualTo("Novo ime"));
            Assert.That(material.Description, Is.EqualTo("Novi opis"));
        });
    }

    [Test]
    public void Constructor_NewMaterialHasNoLastModifiedTime()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Naziv", null);

        Assert.That(material.LastModifiedAt, Is.Null);
    }

    [Test]
    public void UpdateDetails_StoresLastModifiedTime()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);

        material.UpdateDetails("Novo", null, EditTime);

        Assert.That(material.LastModifiedAt, Is.EqualTo(EditTime));
    }

    [Test]
    public void UpdateDetails_RepeatedEditOverwritesLastModifiedTime()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        var later = EditTime.AddMinutes(5);

        material.UpdateDetails("Prvo", null, EditTime);
        material.UpdateDetails("Drugo", null, later);

        Assert.That(material.LastModifiedAt, Is.EqualTo(later));
    }

    [Test]
    public void UpdateDetails_StoresLastModifiedTimeInUtc()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        var localTime = new DateTimeOffset(2026, 11, 10, 11, 0, 0, TimeSpan.FromHours(1));

        material.UpdateDetails("Novo", null, localTime);

        Assert.Multiple(() =>
        {
            Assert.That(material.LastModifiedAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(material.LastModifiedAt, Is.EqualTo(EditTime));
        });
    }

    [Test]
    public void UpdateDetails_RejectsMissingTimeAndKeepsPreviousValues()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");

        Assert.That(
            () => material.UpdateDetails("Novo", "Novi opis", default),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("modifiedAt"));

        Assert.Multiple(() =>
        {
            Assert.That(material.Name, Is.EqualTo("Staro"));
            Assert.That(material.Description, Is.EqualTo("Stari opis"));
            Assert.That(material.LastModifiedAt, Is.Null);
        });
    }

    [Test]
    public void UpdateDetails_DoesNotChangeIdentityOfferingOrPublicationState()
    {
        var id = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var material = new Material(id, offeringId, "Staro", null);

        material.UpdateDetails("Novo", "Opis", EditTime);

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
            () => material.UpdateDetails(name!, "Novi opis", EditTime),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("name"));

        Assert.Multiple(() =>
        {
            Assert.That(material.Name, Is.EqualTo("Staro"));
            Assert.That(material.Description, Is.EqualTo("Stari opis"));
            Assert.That(material.LastModifiedAt, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDetails_AllowsClearingDescription(string? description)
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Naziv", "Opis");

        material.UpdateDetails("Naziv", description, EditTime);

        Assert.That(material.Description, Is.Empty);
    }

    [Test]
    public void UpdateDetails_AcceptsSingleCharacterName()
    {
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Naziv", null);

        material.UpdateDetails("B", null, EditTime);

        Assert.That(material.Name, Is.EqualTo("B"));
    }
}
