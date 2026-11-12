using NUnit.Framework;
using Domain.Courses;

namespace Tests.Domain;

/// <summary>Testovi ponovne aktivacije kataloškog predmeta (ST-003).</summary>
[TestFixture]
public sealed class CourseActivationTests
{
    [Test]
    public void NewCourse_IsActiveByDefault()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera");

        Assert.That(course.IsActive, Is.True);
    }

    [Test]
    public void Activate_MakesInactiveCourseActive()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera", isActive: false);

        course.Activate();

        Assert.That(course.IsActive, Is.True);
    }

    [Test]
    public void Activate_DoesNotChangeCodeNameOrDescription()
    {
        var id = Guid.NewGuid();
        var course = new Course(id, "ERS", "Elementi razvoja softvera", "Opis predmeta", isActive: false);

        course.Activate();

        Assert.Multiple(() =>
        {
            Assert.That(course.Id, Is.EqualTo(id));
            Assert.That(course.Code, Is.EqualTo("ERS"));
            Assert.That(course.Name, Is.EqualTo("Elementi razvoja softvera"));
            Assert.That(course.Description, Is.EqualTo("Opis predmeta"));
        });
    }

    [Test]
    public void Activate_OnActiveCourse_KeepsItActive()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera");

        course.Activate();

        Assert.That(course.IsActive, Is.True);
    }
}
