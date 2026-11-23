using System.Net;
using System.Net.Http.Json;
using Api.Authentication;
using Application.Access;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests;

/// <summary>
/// ST-009, US-10, tačka 1, preko HTTP-a: GET /offerings/{offeringId}/materials.
/// </summary>
[TestFixture]
public sealed class StudentMaterialsListApiTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid EmptyOfferingId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherStudentId = Guid.NewGuid();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Material _published = null!;
    private Material _draft = null!;

    [SetUp]
    public async Task SetUp()
    {
        var materials = new InMemoryMaterialRepository();
        _published = new Material(Guid.NewGuid(), OfferingId, "Predavanje 1", "Uvod u predmet");
        _published.Publish();
        _draft = new Material(Guid.NewGuid(), OfferingId, "Predavanje 2", "Još nije objavljeno");
        await materials.AddAsync(_published);
        await materials.AddAsync(_draft);

        var memberships = new InMemoryMembershipReader(
        [
            new OfferingMembership(StudentId, OfferingId, MembershipKind.Student, true),
            new OfferingMembership(StudentId, EmptyOfferingId, MembershipKind.Student, true),
            new OfferingMembership(OtherStudentId, Guid.NewGuid(), MembershipKind.Student, true)
        ]);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IMaterialRepository>(materials);
                services.AddSingleton<IMembershipReader>(memberships);
            }));
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static string Url(Guid offeringId) => $"/offerings/{offeringId}/materials";

    private void LoginAs(Guid userId) =>
        _client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, userId.ToString());

    [Test]
    public async Task Get_WithoutUser_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(Url(OfferingId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Get_EnrolledStudent_ReceivesOnlyPublishedMaterials()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync(Url(OfferingId));
        var body = await response.Content.ReadFromJsonAsync<StudentMaterialListItem[]>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Has.Length.EqualTo(1));
            Assert.That(body![0].Id, Is.EqualTo(_published.Id));
            Assert.That(body[0].Name, Is.EqualTo("Predavanje 1"));
            Assert.That(body[0].Description, Is.EqualTo("Uvod u predmet"));
        });
    }

    [Test]
    public async Task Get_StudentOfAnotherOffering_IsForbiddenAndGetsNoMaterialNames()
    {
        LoginAs(OtherStudentId);

        var response = await _client.GetAsync(Url(OfferingId));
        var text = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(text, Does.Not.Contain("Predavanje"));
        });
    }

    [Test]
    public async Task Get_OfferingWithoutPublishedMaterials_ReturnsEmptyList()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync(Url(EmptyOfferingId));
        var body = await response.Content.ReadFromJsonAsync<StudentMaterialListItem[]>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Is.Empty);
        });
    }

    [Test]
    public async Task Get_InvalidOfferingId_ReturnsNotFound()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync("/offerings/nije-guid/materials");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
