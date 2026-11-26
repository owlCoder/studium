using System.Net;
using System.Net.Http.Json;
using Application.Activities;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration;

[TestFixture]
public sealed class CreateActivityEndpointTests
{
    private static readonly Guid ExistingOfferingId = DemoActivityOfferings.ErsOfferingId;
    private const string ExistingOfferingSlug = "ERS-00001";
    private const string CatalogCourseSlug = "ERS-11111";
    private const string MissingContextSlug = "XXX-33333";
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

    private static async Task AssertNothingStoredAsync(WebApplicationFactory<Program> factory)
    {
        var repository = factory.Services.GetRequiredService<IActivityRepository>();

        var stored = await repository.ListAsync();

        Assert.That(stored.IsSuccess, Is.True);
        Assert.That(stored.Activities, Is.Empty);
    }

    [Test]
    public async Task PostCreateActivity_ForExistingOfferingSlug_ReturnsCreated()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(ExistingOfferingSlug),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Null);

        var created = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.That(created, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(created!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.ContextId, Is.EqualTo(ExistingOfferingId));
            Assert.That(created.Name, Is.EqualTo("Timski projekat"));
            Assert.That(created.Description, Is.EqualTo("Prva faza"));
            Assert.That(created.StartDate, Is.EqualTo(Start));
            Assert.That(created.EndDate, Is.EqualTo(End));
        });
    }

    [Test]
    public async Task PostCreateActivity_WithCatalogCourseSlug_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(CatalogCourseSlug),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PostCreateActivity_ForUnknownOfferingSlug_ReturnsNotFoundAndDoesNotStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            CreateActivityPath(MissingContextSlug),
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
            CreateActivityPath(ExistingOfferingSlug),
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
            CreateActivityPath(ExistingOfferingSlug),
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
            CreateActivityPath(ExistingOfferingId.ToString()),
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
            CreateActivityPath(ExistingOfferingSlug.ToLowerInvariant()),
            ValidBody());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.That(created!.ContextId, Is.EqualTo(ExistingOfferingId));
    }
}
