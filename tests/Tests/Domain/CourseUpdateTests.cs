using System;
using Domain.Courses;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
internal class CourseUpdateTests
{
    [Test]
    public void Update_ValidData_UpdatesCourseAndNormalizesCode()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv", "Stari opis");
        var offeringLookup = new FakeCourseOfferingLookup();

        course.Update(" ers-2 ", "Novi naziv", "Novi opis", offeringLookup);

        Assert.That(course.Code, Is.EqualTo("ERS-2"));
        Assert.That(course.Name, Is.EqualTo("Novi naziv"));
        Assert.That(course.Description, Is.EqualTo("Novi opis"));
    }

    [Test]
    public void Update_BlankCodeOrName_ThrowsArgumentException()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv");
        var offeringLookup = new FakeCourseOfferingLookup();

        Assert.Throws<ArgumentException>(() => course.Update("", "Novi naziv", "Opis", offeringLookup));
        Assert.Throws<ArgumentException>(() => course.Update("ERS-2", "", "Opis", offeringLookup));
    }

    [Test]
    public void Update_CodeChangedWithActiveOfferings_ThrowsInvalidOperationException()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv");
        var offeringLookup = new FakeCourseOfferingLookup();
        offeringLookup.AddActiveOffering(course.Id);

        Assert.Throws<InvalidOperationException>(() => course.Update("ERS-2", "Novi naziv", "Novi opis", offeringLookup));

        Assert.That(course.Code, Is.EqualTo("ERS-1"));
    }

    [Test]
    public void Update_SameCodeWithActiveOfferings_Succeeds()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv");
        var offeringLookup = new FakeCourseOfferingLookup();
        offeringLookup.AddActiveOffering(course.Id);

        course.Update("ERS-1", "Novi naziv", "Novi opis", offeringLookup);

        Assert.That(course.Code, Is.EqualTo("ERS-1"));
        Assert.That(course.Name, Is.EqualTo("Novi naziv"));
    }

    [Test]
    public void Update_WhenOfferingLookupIsNull_ThrowsArgumentNullException()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv");

        Assert.Throws<ArgumentNullException>(() => course.Update("ERS-2", "Novi naziv", "Opis", null!));
    }

    [Test]
    public void Update_WhenNoActiveOfferingsExist_AllowsCodeChange()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv");
        var offeringLookup = new FakeCourseOfferingLookup();

        course.Update("ERS-2", "Novi naziv", "Novi opis", offeringLookup);

        Assert.That(course.Code, Is.EqualTo("ERS-2"));
        Assert.That(course.Name, Is.EqualTo("Novi naziv"));
        Assert.That(course.Description, Is.EqualTo("Novi opis"));
    }

    [Test]
    public void Update_SameCodeWithDifferentFormatting_DoesNotBlockUpdate()
    {
        var course = new Course(Guid.NewGuid(), "ERS-1", "Stari naziv");
        var offeringLookup = new FakeCourseOfferingLookup();
        offeringLookup.AddActiveOffering(course.Id);

        course.Update(" ers-1 ", "Novi naziv", "Novi opis", offeringLookup);

        Assert.That(course.Code, Is.EqualTo("ERS-1"));
        Assert.That(course.Name, Is.EqualTo("Novi naziv"));
    }
}
