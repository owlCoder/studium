using Application.Activities;
using Application.Courses;
using Domain.Activity;
using Domain.Courses;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class CreateActivityHandlerTests
{
    private static readonly Guid CourseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    private static CreateActivityCommand Command(Guid? courseId = null, string name = "Timski projekat") =>
        new(courseId ?? CourseId, name, "Prva faza", Start, End);

    private static Mock<ICourseReader> CourseReaderWith(params Course[] courses)
    {
        IReadOnlyList<Course> list = courses;

        var reader = new Mock<ICourseReader>();
        reader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        return reader;
    }

    [Test]
    public async Task HandleAsync_CreatesActivityForExistingCourse()
    {
        Activity? stored = null;
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Callback<Activity, CancellationToken>((activity, _) => stored = activity)
            .Returns(Task.CompletedTask);

        var handler = new CreateActivityHandler(
            repository.Object,
            CourseReaderWith(new Course(CourseId, "ERS", "Elementi razvoja softvera")).Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Created));
            Assert.That(result.Activity, Is.Not.Null);
            Assert.That(result.Activity!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Activity.CourseId, Is.EqualTo(CourseId));
            Assert.That(result.Activity.Name, Is.EqualTo("Timski projekat"));
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Id, Is.EqualTo(result.Activity.Id));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_ReturnsCourseNotFoundAndDoesNotStore()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(
            repository.Object,
            CourseReaderWith(new Course(Guid.NewGuid(), "OIB", "Osnove informacione bezbednosti")).Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.CourseNotFound));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Does.Contain(CourseId.ToString()));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenDomainValidationFails()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(
            repository.Object,
            CourseReaderWith(new Course(CourseId, "ERS", "Elementi razvoja softvera")).Object);

        var result = await handler.HandleAsync(Command(name: " "));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenIntervalIsReversed()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(
            repository.Object,
            CourseReaderWith(new Course(CourseId, "ERS", "Elementi razvoja softvera")).Object);

        var result = await handler.HandleAsync(new CreateActivityCommand(
            CourseId, "Aktivnost", "Opis", End, Start));

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_DoesNotSwallowRepositoryFailure()
    {
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Skladište nije dostupno."));

        var handler = new CreateActivityHandler(
            repository.Object,
            CourseReaderWith(new Course(CourseId, "ERS", "Elementi razvoja softvera")).Object);

        Assert.That(
            async () => await handler.HandleAsync(Command()),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var repository = new Mock<IActivityRepository>();
        var reader = CourseReaderWith(new Course(CourseId, "ERS", "Elementi razvoja softvera"));
        var handler = new CreateActivityHandler(repository.Object, reader.Object);
        using var source = new CancellationTokenSource();

        await handler.HandleAsync(Command(), source.Token);

        reader.Verify(x => x.ListAsync(source.Token), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<Activity>(), source.Token), Times.Once);
    }
}
