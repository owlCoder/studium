using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// Regresija: rute koje su postojale pre ST-019/ST-020 (/health, /demo/courses, /courses)
/// moraju ostati dostupne, sa istim ugovorom odgovora, u svim okruzenjima.
/// Testovi idu kroz HTTP nad stvarnim composition root-om.
/// </summary>
[TestFixture]
public class ExistingRoutesRegressionTests
{
    [TestCase("Development")]
    [TestCase("Testing")]
    [TestCase("Production")]
    public async Task Health_ReturnsOk_WithStatusOk(string environment)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("status").GetString(), Is.EqualTo("ok"));
    }

    [TestCase("Development")]
    [TestCase("Testing")]
    [TestCase("Production")]
    public async Task DemoCourses_ReturnsOk_WithSortedCodeAndNameOnly(string environment)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/demo/courses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.EnumerateArray().ToList();

        Assert.That(
            items.Select(item => item.GetProperty("code").GetString()),
            Is.EqualTo(new[] { "ERS", "OIB" }));

        // Ugovor ListCoursesHandler-a: samo Code i Name (bez Id, Description, IsActive).
        foreach (var item in items)
        {
            Assert.That(
                item.EnumerateObject().Select(property => property.Name),
                Is.EquivalentTo(new[] { "code", "name" }));
        }
    }

    [TestCase("Development")]
    [TestCase("Testing")]
    [TestCase("Production")]
    public async Task Courses_WithoutQuery_ReturnsAllCourses(string environment)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/courses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.EnumerateArray().ToList();

        Assert.That(
            items.Select(item => item.GetProperty("code").GetString()),
            Is.EqualTo(new[] { "ERS", "OIB" }));

        // Ugovor SearchCoursesHandler-a: Id, Code, Name.
        foreach (var item in items)
        {
            Assert.That(
                item.EnumerateObject().Select(property => property.Name),
                Is.EquivalentTo(new[] { "id", "code", "name" }));
        }
    }

    [TestCase("Development", "ers")]
    [TestCase("Testing", "ERS")]
    [TestCase("Production", "razvoja")]
    public async Task Courses_WithQuery_FiltersByCodeOrName(string environment, string query)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/courses?query={query}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var codes = json.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .ToList();

        Assert.That(codes, Is.EqualTo(new[] { "ERS" }));
    }

    [TestCase("Development")]
    [TestCase("Production")]
    public async Task Courses_WithNonMatchingQuery_ReturnsEmptyArray(string environment)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/courses?query=nepostojeci-predmet");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public async Task Development_ExistingAndNewRoutesCoexist()
    {
        using var factory = ApiTestHost.Create("Development");
        using var client = factory.CreateClient();

        // Postojece rute i dalje rade...
        Assert.That((await client.GetAsync("/health")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await client.GetAsync("/demo/courses")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await client.GetAsync("/courses")).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // ...a ST-019 / ST-020 rute postoje (anonimni poziv se odbija sa 401, a ne 404).
        var groups = await client.PostAsJsonAsync(
            $"/groups/{Guid.NewGuid()}/students",
            new { StudentId = Guid.NewGuid(), StartDate = DateTime.UtcNow });
        Assert.That(groups.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var audience = await client.PostAsJsonAsync(
            $"/contents/{Guid.NewGuid()}/target-audience",
            new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() });
        Assert.That(audience.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Production_DevelopmentOnlyAudienceEndpoint_IsNotExposed()
    {
        using var factory = ApiTestHost.Create("Production");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/contents/{Guid.NewGuid()}/target-audience",
            new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
