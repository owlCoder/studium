using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests.Integration;

[TestFixture]
public sealed class ActivityEndpointsTests
{
    private const string ListActivitiesPath = "/ListActivities";
    private static readonly Guid ExistingCourseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MissingCourseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    private sealed record CreateActivityBody(
        string Name,
        string Description,
        DateTime StartDate,
        DateTime EndDate);

    private sealed record ActivityResponse(
        Guid Id,
        Guid CourseId,
        string Name,
        string Description,
        DateTime StartDate,
        DateTime EndDate);

    private static string CreateActivityPath(Guid courseId) => $"/Course/{courseId}/CreateActivity";

    private static CreateActivityBody ValidBody(
        string name = "Timski projekat",
        DateTime? startDate = null,
        DateTime? endDate = null) =>
        new(name, "Prva faza", startDate ?? Start, endDate ?? End);

    private static WebApplicationFactory<Program> CreateFactory() => new();

    [Test]
    public async Task PostCreateActivity_ForExistingCourse_ReturnsCreatedAndIsListed()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseId),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo(ListActivitiesPath));

        var created = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.That(created, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(created!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.CourseId, Is.EqualTo(ExistingCourseId));
            Assert.That(created.Name, Is.EqualTo("Timski projekat"));
            Assert.That(created.Description, Is.EqualTo("Prva faza"));
            Assert.That(created.StartDate, Is.EqualTo(Start));
            Assert.That(created.EndDate, Is.EqualTo(End));
        });

        var list = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);

        Assert.That(list, Is.Not.Null);
        Assert.That(list, Has.Length.EqualTo(1));
        Assert.That(list![0].Id, Is.EqualTo(created!.Id));
    }

    [Test]
    public async Task GetListActivities_WhenThereAreNoActivities_ReturnsEmptyArray()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ListActivitiesPath);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var activities = await response.Content.ReadFromJsonAsync<ActivityResponse[]>();
        Assert.That(activities, Is.Empty);
    }

    [Test]
    public async Task PostCreateActivity_ForMissingCourse_ReturnsNotFoundAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(MissingCourseId),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var activities = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);
        Assert.That(activities, Is.Empty);
    }

    [Test]
    public async Task PostCreateActivity_WithBlankName_ReturnsBadRequestAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseId),
            ValidBody(name: " "));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var activities = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);
        Assert.That(activities, Is.Empty);
    }

    [Test]
    public async Task PostCreateActivity_WithReversedInterval_ReturnsBadRequestAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseId),
            ValidBody(startDate: End, endDate: Start));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var activities = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);
        Assert.That(activities, Is.Empty);
    }

    [Test]
    public async Task PostCreateActivity_WithNonGuidCourseId_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/Course/not-a-guid/CreateActivity",
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetListActivities_ReturnsActivitiesOrderedByStartDate()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var later = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseId),
            ValidBody(name: "Kasnija", startDate: Start.AddDays(10), endDate: End.AddDays(10)));
        var earlier = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseId),
            ValidBody(name: "Ranija"));

        Assert.Multiple(() =>
        {
            Assert.That(later.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(earlier.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        });

        var activities = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);

        Assert.That(activities, Is.Not.Null);
        Assert.That(activities!.Select(activity => activity.Name),
            Is.EqualTo(new[] { "Ranija", "Kasnija" }));
    }
}
