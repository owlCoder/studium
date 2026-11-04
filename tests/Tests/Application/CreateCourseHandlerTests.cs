
using Moq;
using NUnit.Framework;
using Application.Courses;
using Domain.Courses;

namespace Tests.Application;

[TestFixture]
public sealed class CreateCourseHandlerTests
{
    private Mock<ICourseRepository> _repository = null!;
    private CreateCourseHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ICourseRepository>();
        _handler = new CreateCourseHandler(_repository.Object);
    }

    [Test]
    public async Task HandleAsync_ValidCommand_CreatesCourse()
    {
        var command = new CreateCourseCommand(
            "ers",
            "Elementi razvoja softvera",
            "Predmet koji obrađuje osnove razvoja softvera.");

        _repository
            .Setup(x => x.ExistsByCodeAsync(
                "ERS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var courseId = await _handler.HandleAsync(command);

        Assert.That(courseId, Is.Not.EqualTo(Guid.Empty));

        _repository.Verify(x => x.ExistsByCodeAsync(
            "ERS", It.IsAny<CancellationToken>()), Times.Once);

        _repository.Verify(x => x.AddAsync(
            It.Is<Course>(c =>
                c.Id == courseId &&
                c.Code == "ERS" &&
                c.Name == "Elementi razvoja softvera" &&
                c.Description ==
                    "Predmet koji obrađuje osnove razvoja softvera." &&
                c.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ExistingCode_ThrowsException()
    {
        var command = new CreateCourseCommand(
            "ERS",
            "Elementi razvoja softvera",
            "Predmet koji obrađuje osnove razvoja softvera.");

        _repository
            .Setup(x => x.ExistsByCodeAsync(
                "ERS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.HandleAsync(command));

        _repository.Verify(x => x.AddAsync(
            It.IsAny<Course>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void HandleAsync_EmptyCode_ThrowsException()
    {
        var command = new CreateCourseCommand(
            "",
            "Elementi razvoja softvera",
            "Predmet koji obrađuje osnove razvoja softvera.");

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.HandleAsync(command));

        _repository.Verify(x => x.ExistsByCodeAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(x => x.AddAsync(
            It.IsAny<Course>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void HandleAsync_EmptyName_ThrowsException()
    {
        var command = new CreateCourseCommand(
            "ERS",
            "",
            "Predmet koji obrađuje osnove razvoja softvera.");

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.HandleAsync(command));

        _repository.Verify(x => x.ExistsByCodeAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(x => x.AddAsync(
            It.IsAny<Course>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_NormalizesCodeBeforeCheckingExistence()
    {
        var command = new CreateCourseCommand(
            " ers ",
            "Elementi razvoja softvera",
            "Predmet koji obrađuje osnove razvoja softvera.");

        _repository
            .Setup(x => x.ExistsByCodeAsync(
                "ERS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync(command);

        _repository.Verify(x => x.ExistsByCodeAsync(
            "ERS", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ValidCommand_PassesCancellationToken()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken = cancellationTokenSource.Token;

        var command = new CreateCourseCommand(
            "ERS",
            "Elementi razvoja softvera",
            "Predmet koji obrađuje osnove razvoja softvera.");

        _repository
            .Setup(x => x.ExistsByCodeAsync(
                "ERS", cancellationToken))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<Course>(), cancellationToken))
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync(command, cancellationToken);

        _repository.Verify(x => x.ExistsByCodeAsync(
            "ERS", cancellationToken), Times.Once);

        _repository.Verify(x => x.AddAsync(
            It.IsAny<Course>(), cancellationToken), Times.Once);
    }
}
