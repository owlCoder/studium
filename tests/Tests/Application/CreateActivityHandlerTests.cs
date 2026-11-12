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
    private const string CourseSlug = "ERS-11111";
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    private static CreateActivityCommand Command(
        string courseSlug = CourseSlug,
        string name = "Timski projekat") =>
        new(courseSlug, name, "Prva faza", Start, End);

    private static Mock<ICourseReader> CourseReaderWith(params Course[] courses)
    {
        IReadOnlyList<Course> list = courses;

        var reader = new Mock<ICourseReader>();
        reader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        return reader;
    }

    private static Mock<ICourseReader> ExistingCourseReader() =>
        CourseReaderWith(new Course(CourseId, "ERS", "Elementi razvoja softvera"));

    private static Mock<IActivityRepository> RepositoryReturning(ActivityAddResult outcome)
    {
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);
        return repository;
    }

    [Test]
    public async Task HandleAsync_CreatesActivityForExistingCourse()
    {
        Activity? stored = null;
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Callback<Activity?, CancellationToken>((activity, _) => stored = activity)
            .ReturnsAsync(ActivityAddResult.Added());

        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

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
            CourseReaderWith(new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "OIB", "Osnove informacione bezbednosti")).Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.CourseNotFound));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Does.Contain(CourseSlug));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenDomainValidationFails()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

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
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

        var result = await handler.HandleAsync(new CreateActivityCommand(
            CourseSlug, "Aktivnost", "Opis", End, Start));

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsConflictWhenStoreReportsDuplicateId()
    {
        var repository = RepositoryReturning(ActivityAddResult.DuplicateId());
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Conflict));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsCanceledWhenStoreCancelsTheOperation()
    {
        var repository = RepositoryReturning(ActivityAddResult.Canceled());
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Canceled));
            Assert.That(result.Activity, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenStoreRejectsActivity()
    {
        var repository = RepositoryReturning(ActivityAddResult.InvalidActivity());
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

        var result = await handler.HandleAsync(Command());

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
    }

    [Test]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var repository = RepositoryReturning(ActivityAddResult.Added());
        var reader = ExistingCourseReader();
        var handler = new CreateActivityHandler(repository.Object, reader.Object);
        using var source = new CancellationTokenSource();

        await handler.HandleAsync(Command(), source.Token);

        reader.Verify(x => x.ListAsync(source.Token), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<Activity>(), source.Token), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ResolvesSlugRegardlessOfCase()
    {
        var repository = RepositoryReturning(ActivityAddResult.Added());
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

        var result = await handler.HandleAsync(Command("ers-11111"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Created));
            Assert.That(result.Activity!.CourseId, Is.EqualTo(CourseId));
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotResolveRawIdentifier()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(repository.Object, ExistingCourseReader().Object);

        var result = await handler.HandleAsync(Command(CourseId.ToString()));

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.CourseNotFound));
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenSlugIsAmbiguous()
    {
        var repository = new Mock<IActivityRepository>();
        var courses = new[]
        {
            new Course(CourseId, "ERS", "Elementi razvoja softvera"),
            new Course(Guid.Parse("22222222-2222-2222-2222-111111111111"), "ERS", "Drugi predmet")
        };
        var handler = new CreateActivityHandler(repository.Object, CourseReaderWith(courses).Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
            Assert.That(result.Error, Does.Contain(CourseSlug));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
