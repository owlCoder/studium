using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Courses;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
internal class CourseUpdateTests
{
    [Test]
    public void Update_ValidData_UpdatesCourseAndNormalizesCode()
    {
        var course = new Course(Guid.NewGuid(), "ers-1", "Stari naziv", "Stari opis");
        var offeringLookupMock = Substitute.For<ICourseOfferingLookup>();
        offeringLookupMock.HasOfferings(course.Id).Returns(false);

        course.Update("  ers-2  ", "Novi naziv", "  Novi opis  ", offeringLookupMock);

        Assert.Multiple(() =>
        {
            Assert.That(course.Code, Is.EqualTo("ERS-2"));
            Assert.That(course.Name, Is.EqualTo("Novi naziv"));
            Assert.That(course.Description, Is.EqualTo("Novi opis"));
        });
    }

    [Test]
    public void Update_BlankCodeOrName_ThrowsArgumentException()
    {
        var course = new Course(Guid.NewGuid(), "ers-1", "Naziv", "Opis");
        var offeringLookupMock = Substitute.For<ICourseOfferingLookup>();

        Assert.That(
            () => course.Update("   ", "Novi naziv", "Opis", offeringLookupMock),
            Throws.ArgumentException);

        Assert.That(
            () => course.Update("ers-2", "", "Opis", offeringLookupMock),
            Throws.ArgumentException);
    }

    [Test]
    public void Update_CodeChangedWithActiveOfferings_ThrowsInvalidOperationException()
    {
        var course = new Course(Guid.NewGuid(), "ers-1", "Naziv", "Opis");
        var offeringLookupMock = Substitute.For<ICourseOfferingLookup>();
        offeringLookupMock.HasOfferings(course.Id).Returns(true);

        Assert.That(
            () => course.Update("ers-2", "Novi naziv", "Opis", offeringLookupMock),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Update_SameCodeWithActiveOfferings_Succeeds()
    {
        var course = new Course(Guid.NewGuid(), "ers-1", "Naziv", "Opis");
        var offeringLookupMock = Substitute.For<ICourseOfferingLookup>();
        offeringLookupMock.HasOfferings(course.Id).Returns(true);

        course.Update("ers-1", "Ažurirani naziv", "Ažurirani opis", offeringLookupMock);

        Assert.Multiple(() =>
        {
            Assert.That(course.Name, Is.EqualTo("Ažurirani naziv"));
            Assert.That(course.Description, Is.EqualTo("Ažurirani opis"));
        });
    }

    [Test]
    public void Update_WhenOfferingLookupIsNull_ThrowsArgumentNullException()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera", "Opis");

        Assert.That(
            () => course.Update("ERS101", "Elementi razvoja softvera", "Opis", null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Update_WhenNoActiveOfferingsExist_AllowsCodeChange()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera", "Opis");
        var offeringLookupMock = Substitute.For<ICourseOfferingLookup>();
        offeringLookupMock.HasOfferings(course.Id).Returns(false);

        course.Update("ERS-NEW", "Novi naziv", "Novi opis", offeringLookupMock);

        Assert.Multiple(() =>
        {
            Assert.That(course.Code, Is.EqualTo("ERS-NEW"));
            Assert.That(course.Name, Is.EqualTo("Novi naziv"));
        });
    }
}
