
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Courses;
using Domain.Courses;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
internal class UpdateCourseHandlerTests
{
    private Mock<ICourseRepository> _repositoryMock = null!;
    private Mock<ICourseOfferingLookup> _offeringLookupMock = null!;
    private UpdateCourseHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<ICourseRepository>();
        _offeringLookupMock = new Mock<ICourseOfferingLookup>();
        _handler = new UpdateCourseHandler(_repositoryMock.Object, _offeringLookupMock.Object);
    }

    [Test]
    public async Task HandleAsync_ValidData_UpdatesCourse()
    {
        var courseId = Guid.NewGuid();
        var course = new Course(courseId, "ERS-1", "Stari naziv", "Stari opis");
        var command = new UpdateCourseCommand(courseId, "ERS-2", "Novi naziv", "Novi opis");

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _repositoryMock.Setup(r => r.ExistsByCodeAsync("ERS-2", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _offeringLookupMock.Setup(l => l.HasActiveOfferings(courseId)).Returns(false);

        await _handler.HandleAsync(command);

        Assert.That(course.Code, Is.EqualTo("ERS-2"));
        Assert.That(course.Name, Is.EqualTo("Novi naziv"));
        Assert.That(course.Description, Is.EqualTo("Novi opis"));

        _repositoryMock.Verify(r => r.UpdateAsync(course, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_CourseDoesNotExist_ThrowsKeyNotFoundException()
    {
        var courseId = Guid.NewGuid();
        var command = new UpdateCourseCommand(courseId, "ERS-2", "Novi naziv", "Novi opis");

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync((Course?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _handler.HandleAsync(command));

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_CodeAlreadyExists_ThrowsInvalidOperationException()
    {
        var courseId = Guid.NewGuid();
        var course = new Course(courseId, "ERS-1", "Stari naziv", "Stari opis");
        var command = new UpdateCourseCommand(courseId, "ERS-2", "Novi naziv", "Novi opis");

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _repositoryMock.Setup(r => r.ExistsByCodeAsync("ERS-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _handler.HandleAsync(command));

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_ActiveOfferingsAndChangedCode_ThrowsInvalidOperationException()
    {
        var courseId = Guid.NewGuid();
        var course = new Course(courseId, "ERS-1", "Stari naziv", "Stari opis");
        var command = new UpdateCourseCommand(courseId, "ERS-2", "Novi naziv", "Novi opis");

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _repositoryMock.Setup(r => r.ExistsByCodeAsync("ERS-2", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _offeringLookupMock.Setup(l => l.HasActiveOfferings(courseId)).Returns(true);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _handler.HandleAsync(command));

        Assert.That(course.Code, Is.EqualTo("ERS-1"));
        Assert.That(course.Name, Is.EqualTo("Stari naziv"));
        Assert.That(course.Description, Is.EqualTo("Stari opis"));

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_SameCode_DoesNotCheckForDuplicate()
    {
        var courseId = Guid.NewGuid();
        var course = new Course(courseId, "ERS-1", "Stari naziv", "Stari opis");
        var command = new UpdateCourseCommand(courseId, "ERS-1", "Novi naziv", "Novi opis");

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _offeringLookupMock.Setup(l => l.HasActiveOfferings(courseId)).Returns(true);

        await _handler.HandleAsync(command);

        Assert.That(course.Name, Is.EqualTo("Novi naziv"));
        Assert.That(course.Description, Is.EqualTo("Novi opis"));

        _repositoryMock.Verify(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(course, It.IsAny<CancellationToken>()), Times.Once);
    }
}
