using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests.Integration;

[TestFixture]
public sealed class ListActivitiesEndpointTests
{
    private const string ListActivitiesPath = "/ListActivities";
    private const string ExistingCourseSlug = "ERS-11111";
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    private sealed record CreateActivityBody(
        string Name,
        string Description,
        DateTime StartDate,
        DateTime EndDate);

    private sealed record ActivityResponse(
        Guid Id,
        Guid ContextId,
        string Name,
        string Description,
        DateTime StartDate,
        DateTime EndDate);

    private static string CreateActivityPath(string contextSlug) => $"/Course/{contextSlug}/CreateActivity";

    private static CreateActivityBody ValidBody(
        string name = "Timski projekat",
        DateTime? startDate = null,
        DateTime? endDate = null) =>
        new(name, "Prva faza", startDate ?? Start, endDate ?? End);

    private static WebApplicationFactory<Program> CreateFactory() => new();

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
    public async Task GetListActivities_ReturnsCreatedActivity()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug),
            ValidBody());
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var posted = await created.Content.ReadFromJsonAsync<ActivityResponse>();

        var activities = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);

        Assert.That(activities, Is.Not.Null);
        Assert.That(activities, Has.Length.EqualTo(1));
        Assert.That(activities![0].Id, Is.EqualTo(posted!.Id));
        Assert.That(activities[0].Name, Is.EqualTo("Timski projekat"));
    }

    [Test]
    public async Task GetListActivities_ReturnsActivitiesOrderedByStartDate()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var later = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug),
            ValidBody(name: "Kasnija", startDate: Start.AddDays(10), endDate: End.AddDays(10)));
        var earlier = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug),
            ValidBody(name: "Ranija"));

        Assert.Multiple(() =>
        {
            Assert.That(later.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(earlier.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        });

        var activities = await client.GetFromJsonAsync<ActivityResponse[]>(ListActivitiesPath);

        Assert.That(activities, Is.Not.Null);
        Assert.That(
            activities!.Select(activity => activity.Name),
            Is.EqualTo(new[] { "Ranija", "Kasnija" }));
    }
}
