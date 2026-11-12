using Moq;
using NUnit.Framework;
using Application.Courses;
using Domain.Courses;

namespace Tests.Application;

/// <summary>Testovi slučaja upotrebe ponovne aktivacije predmeta (ST-003).</summary>
[TestFixture]
public sealed class ActivateCourseHandlerTests
{
    private static Course InactiveErs() =>
        new(Guid.NewGuid(), "ERS", "Elementi razvoja softvera", "Opis predmeta", isActive: false);

    private static (ActivateCourseHandler Handler, Mock<ICourseStatusRepository> Repository) Create(
        Course? stored)
    {
        var repository = new Mock<ICourseStatusRepository>();
        repository.Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        repository.Setup(x => x.SaveAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new ActivateCourseHandler(repository.Object), repository);
    }

    [Test]
    public async Task HandleAsync_ActivatesInactiveCourse_AndSavesIt()
    {
        var course = InactiveErs();
        var (handler, repository) = Create(course);

        var result = await handler.HandleAsync(course.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ActivateCourseResult.Activated));
            Assert.That(course.IsActive, Is.True);
        });
        repository.Verify(x => x.FindByIdAsync(course.Id, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveAsync(course, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_DoesNotSave_WhenCourseIsAlreadyActive()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera");
        var (handler, repository) = Create(course);

        var result = await handler.HandleAsync(course.Id);

        Assert.That(result, Is.EqualTo(ActivateCourseResult.AlreadyActive));
        repository.Verify(x => x.SaveAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        var (handler, repository) = Create(stored: null);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.That(result, Is.EqualTo(ActivateCourseResult.NotFound));
        repository.Verify(x => x.SaveAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_PreservesCatalogRecord_OfReactivatedCourse()
    {
        // Primer iz specifikacije: predmet ERS ostaje isti zapis, menja se samo aktivnost.
        var course = InactiveErs();
        var id = course.Id;
        var (handler, _) = Create(course);

        await handler.HandleAsync(id);

        Assert.Multiple(() =>
        {
            Assert.That(course.Id, Is.EqualTo(id));
            Assert.That(course.Code, Is.EqualTo("ERS"));
            Assert.That(course.Name, Is.EqualTo("Elementi razvoja softvera"));
            Assert.That(course.Description, Is.EqualTo("Opis predmeta"));
        });
    }

    [Test]
    public void HandleAsync_DoesNotSwallowRepositoryFailure()
    {
        var repository = new Mock<ICourseStatusRepository>();
        repository.Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Izvor podataka nije dostupan."));

        var handler = new ActivateCourseHandler(repository.Object);

        Assert.That(
            async () => await handler.HandleAsync(Guid.NewGuid()),
            Throws.TypeOf<InvalidOperationException>());
    }
}
