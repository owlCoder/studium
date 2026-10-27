using NUnit.Framework;
using Domain.Courses;

namespace Tests.Domain;

[TestFixture]
public sealed class CourseTests
{
    [Test]
    public void Constructor_NormalizesCodeAndName()
    {
        var course = new Course(Guid.NewGuid(), " ers ", " Elementi razvoja softvera ");

        Assert.Multiple(() =>
        {
            Assert.That(course.Code, Is.EqualTo("ERS"));
            Assert.That(course.Name, Is.EqualTo("Elementi razvoja softvera"));
        });
    }

    [Test]
    public void Constructor_RejectsBlankCode()
    {
        Assert.That(
            () => new Course(Guid.NewGuid(), " ", "Predmet"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.That(
            () => new Course(Guid.Empty, "ERS", "Predmet"),
            Throws.TypeOf<ArgumentException>());
    }
}
