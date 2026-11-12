using Application.Activities;
using Domain.Activity;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class ListActivitiesHandlerTests
{
    private static readonly Guid CourseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Activity ActivityOf(string name, DateTime start) =>
        Activity.TryCreate(Guid.NewGuid(), CourseId, name, "Opis", start, start.AddDays(7)).Activity!;

    private static (ListActivitiesHandler Handler, Mock<IActivityRepository> Repository) Create(
        params Activity[] activities)
    {
        IReadOnlyList<Activity> list = activities;

        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActivityListResult.Success(list));
        return (new ListActivitiesHandler(repository.Object), repository);
    }

    [Test]
    public async Task HandleAsync_ReturnsAllActivitiesSortedByStartDate()
    {
        var (handler, _) = Create(
            ActivityOf("Kasnija", Start.AddDays(10)),
            ActivityOf("Ranija", Start));

        var result = await handler.HandleAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Activities.Select(x => x.Name), Is.EqualTo(new[] { "Ranija", "Kasnija" }));
    }

    [Test]
    public async Task HandleAsync_ReturnsEmptyCollectionWhenThereAreNoActivities()
    {
        var (handler, _) = Create();

        var result = await handler.HandleAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Activities, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_MapsAllActivityFields()
    {
        var activity = ActivityOf("Timski projekat", Start);
        var (handler, _) = Create(activity);

        var result = await handler.HandleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Activities[0].Id, Is.EqualTo(activity.Id));
            Assert.That(result.Activities[0].CourseId, Is.EqualTo(activity.CourseId));
            Assert.That(result.Activities[0].Name, Is.EqualTo(activity.Name));
            Assert.That(result.Activities[0].Description, Is.EqualTo(activity.Description));
            Assert.That(result.Activities[0].StartDate, Is.EqualTo(activity.StartDate));
            Assert.That(result.Activities[0].EndDate, Is.EqualTo(activity.EndDate));
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsCanceledWhenStoreCancelsTheOperation()
    {
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActivityListResult.Canceled());
        var handler = new ListActivitiesHandler(repository.Object);

        var result = await handler.HandleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ListActivitiesStatus.Canceled));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Activities, Is.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var (handler, repository) = Create(ActivityOf("Aktivnost", Start));
        using var source = new CancellationTokenSource();

        await handler.HandleAsync(source.Token);

        repository.Verify(x => x.ListAsync(source.Token), Times.Once);
    }
}
