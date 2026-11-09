
using Moq;
using NUnit.Framework;
using Application.Courses;
using Domain.Courses;

namespace Tests.Application;

[TestFixture]
public sealed class CreateCourseHandlerTests
{
    [Test]
    public async Task HandleAsync_CreatesCourseWhenCodeDoesNotExist()
    {
        var command = new CreateCourseCommand(
            "ERS",
            "Elementi razvoja softvera",
            "Opis predmeta");

        var repository = new Mock<ICourseRepository>();

        repository.Setup(x => x.ExistsByCodeAsync(
                "ERS",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repository.Setup(x => x.AddAsync(
                It.IsAny<Course>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateCourseHandler(repository.Object);

        var result = await handler.HandleAsync(command);

        Assert.That(result, Is.Not.EqualTo(Guid.Empty));

        repository.Verify(x => x.ExistsByCodeAsync(
            "ERS",
            It.IsAny<CancellationToken>()), Times.Once);

        repository.Verify(x => x.AddAsync(
            It.Is<Course>(c =>
                c.Id == result &&
                c.Code == "ERS" &&
                c.Name == "Elementi razvoja softvera"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ThrowsExceptionWhenCodeAlreadyExists()
    {
        var command = new CreateCourseCommand(
            "ERS",
            "Elementi razvoja softvera",
            "");

        var repository = new Mock<ICourseRepository>();

        repository.Setup(x => x.ExistsByCodeAsync(
                "ERS",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateCourseHandler(repository.Object);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(command));

        repository.Verify(x => x.ExistsByCodeAsync(
            "ERS",
            It.IsAny<CancellationToken>()), Times.Once);

        repository.Verify(x => x.AddAsync(
            It.IsAny<Course>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_NormalizesCourseCode()
    {
        var command = new CreateCourseCommand(
            "  ers  ",
            "Elementi razvoja softvera",
            "");

        var repository = new Mock<ICourseRepository>();

        repository.Setup(x => x.ExistsByCodeAsync(
                "ERS",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repository.Setup(x => x.AddAsync(
                It.IsAny<Course>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateCourseHandler(repository.Object);

        await handler.HandleAsync(command);

        repository.Verify(x => x.ExistsByCodeAsync(
            "ERS",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase("", "Valid name")]
    [TestCase("ERS", "")]
    [TestCase("ERS", "   ")]
    public void HandleAsync_ThrowsExceptionWhenRequiredDataIsInvalid(
        string code,
        string name)
    {
        var command = new CreateCourseCommand(code, name, "");

        var repository = new Mock<ICourseRepository>();
        var handler = new CreateCourseHandler(repository.Object);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.HandleAsync(command));

        repository.Verify(x => x.ExistsByCodeAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        repository.Verify(x => x.AddAsync(
            It.IsAny<Course>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
