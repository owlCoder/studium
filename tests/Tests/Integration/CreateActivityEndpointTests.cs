using System.Net;
using System.Net.Http.Json;
using Application.Activities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration;

[TestFixture]
public sealed class CreateActivityEndpointTests
{
    private const string CreatedLocation = "/ListActivities";
    private static readonly Guid ExistingContextId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ExistingCourseSlug = "ERS-11111";
    private const string MissingCourseSlug = "XXX-33333";
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

    private static string CreateActivityPath(string courseSlug) => $"/Course/{courseSlug}/CreateActivity";

    private static CreateActivityBody ValidBody(
        string name = "Timski projekat",
        DateTime? startDate = null,
        DateTime? endDate = null) =>
        new(name, "Prva faza", startDate ?? Start, endDate ?? End);

    private static WebApplicationFactory<Program> CreateFactory() => new();

    private static async Task AssertNothingStoredAsync(WebApplicationFactory<Program> factory)
    {
        var repository = factory.Services.GetRequiredService<IActivityRepository>();

        var stored = await repository.ListAsync();

        Assert.That(stored.IsSuccess, Is.True);
        Assert.That(stored.Activities, Is.Empty);
    }

    [Test]
    public async Task PostCreateActivity_ForExistingCourseSlug_ReturnsCreated()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo(CreatedLocation));

        var created = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.That(created, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(created!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.ContextId, Is.EqualTo(ExistingContextId));
            Assert.That(created.Name, Is.EqualTo("Timski projekat"));
            Assert.That(created.Description, Is.EqualTo("Prva faza"));
            Assert.That(created.StartDate, Is.EqualTo(Start));
            Assert.That(created.EndDate, Is.EqualTo(End));
        });
    }

    [Test]
    public async Task PostCreateActivity_ForUnknownCourseSlug_ReturnsNotFoundAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(MissingCourseSlug),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertNothingStoredAsync(factory);
    }

    [Test]
    public async Task PostCreateActivity_WithBlankName_ReturnsBadRequestAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug),
            ValidBody(name: " "));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertNothingStoredAsync(factory);
    }

    [Test]
    public async Task PostCreateActivity_WithReversedInterval_ReturnsBadRequestAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug),
            ValidBody(startDate: End, endDate: Start));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertNothingStoredAsync(factory);
    }

    [Test]
    public async Task PostCreateActivity_WithRawIdentifier_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingContextId.ToString()),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertNothingStoredAsync(factory);
    }

    [Test]
    public async Task PostCreateActivity_WithLowercaseSlug_ReturnsCreated()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingCourseSlug.ToLowerInvariant()),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.That(created!.ContextId, Is.EqualTo(ExistingContextId));
    }
}
