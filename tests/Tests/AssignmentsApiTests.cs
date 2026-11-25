using System.Net;
using System.Net.Http.Json;
using Api.Authentication;
using Application.Assignments;
using Domain.Assignments;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public sealed class AssignmentsApiTests
{
    private static readonly DateTimeOffset Deadline =
        new(2027, 1, 15, 18, 30, 0, TimeSpan.FromHours(1));

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Create_ReturnsCreatedAndStoresDraftForAssignedTeacher()
    {
        LoginAs(DemoData.ErsTeacherId);

        using var response = await _client.PostAsJsonAsync(AssignmentsUrl(DemoData.ErsId), new
        {
            title = "Domaći zadatak",
            description = "Opis zadatka",
            instructions = "Uputstvo za rad",
            deadline = Deadline
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var result = await response.Content.ReadFromJsonAsync<CreateAssignmentResponse>();
        Assert.That(result!.AssignmentId, Is.Not.EqualTo(Guid.Empty));

        var repository = _factory.Services.GetRequiredService<IAssignmentRepository>();
        var draft = await repository.FindAsync(result.AssignmentId);
        Assert.That(draft, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(draft!.Status, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(draft.CourseOfferingId, Is.EqualTo(DemoData.ErsId));
            Assert.That(draft.Title, Is.EqualTo("Domaći zadatak"));
            Assert.That(draft.Description, Is.EqualTo("Opis zadatka"));
            Assert.That(draft.Instructions, Is.EqualTo("Uputstvo za rad"));
            Assert.That(draft.Deadline?.EqualsExact(Deadline), Is.True);
        });
    }

    [TestCase("33333333-0000-0000-0000-000000000001", true, HttpStatusCode.Created)]
    [TestCase("33333333-0000-0000-0000-000000000002", true, HttpStatusCode.Created)]
    [TestCase("33333333-0000-0000-0000-000000000002", false, HttpStatusCode.Forbidden)]
    [TestCase("33333333-0000-0000-0000-000000000001", false, HttpStatusCode.Forbidden)]
    public async Task Create_WithSessionChecksActiveTeachingRoleOnOffering(
        string offeringId, bool isTeacher, HttpStatusCode expectedStatus)
    {
        var userId = isTeacher ? DemoData.DemoTeacherId : DemoData.DemoAssistantId;
        var session = Guid.NewGuid().ToString("N");
        _factory.Services.GetRequiredService<InMemoryAccountStatusReader>().Activate(userId);
        _factory.Services.GetRequiredService<InMemorySessionReader>().Register(session, userId);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", session);

        using var response = await _client.PostAsJsonAsync(
            AssignmentsUrl(Guid.Parse(offeringId)), new { title = "Domaći zadatak" });

        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
    }

    [Test]
    public async Task Create_ReturnsUnauthorizedWithoutIdentity()
    {
        using var response = await _client.PostAsJsonAsync(
            AssignmentsUrl(DemoData.ErsId), new { title = "Zadatak" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Create_ReturnsForbiddenForTeacherOnAnotherOffering()
    {
        LoginAs(DemoData.ErsTeacherId);
        using var response = await _client.PostAsJsonAsync(
            AssignmentsUrl(DemoData.OibId), new { title = "Zadatak" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Create_ReturnsForbiddenForUserWithoutTeacherRole()
    {
        LoginAs(Guid.NewGuid());
        using var response = await _client.PostAsJsonAsync(
            AssignmentsUrl(DemoData.ErsId), new { title = "Zadatak" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Create_ReturnsBadRequestForBlankTitle()
    {
        LoginAs(DemoData.ErsTeacherId);
        using var response = await _client.PostAsJsonAsync(
            AssignmentsUrl(DemoData.ErsId), new { title = " " });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_ReturnsUnauthorizedWithDemoIdentityInProduction()
    {
        using var productionFactory = _factory.WithWebHostBuilder(
            builder => builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient();
        client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, DemoData.ErsTeacherId.ToString());

        using var response = await client.PostAsJsonAsync(
            AssignmentsUrl(DemoData.ErsId), new { title = "Zadatak" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private void LoginAs(Guid userId)
    {
        _client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, userId.ToString());
    }

    private static string AssignmentsUrl(Guid offeringId) => $"/api/offerings/{offeringId}/assignments";

    private sealed record CreateAssignmentResponse(Guid AssignmentId);
}
