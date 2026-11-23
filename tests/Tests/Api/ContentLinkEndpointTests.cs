using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Domain.Activities;
using Infrastructure.Activities;
using Infrastructure.Dashboard;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Api;


[TestFixture]
public sealed class ContentLinkEndpointTests
{
    private static readonly Guid Ers = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private WebApplicationFactory<Program> _factory = null!;
    private InMemorySessionReader _sessions = null!;
    private InMemoryAccountStatusReader _accounts = null!;
    private InMemoryActivityStore _activities = null!;
    private Guid _published;
    private Guid _draft;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        _sessions = _factory.Services.GetRequiredService<InMemorySessionReader>();
        _accounts = _factory.Services.GetRequiredService<InMemoryAccountStatusReader>();
        _activities = _factory.Services.GetRequiredService<InMemoryActivityStore>();

        var now = DateTimeOffset.UtcNow;
        _published = Guid.NewGuid();
        _draft = Guid.NewGuid();
        _activities.AddActivity(new Activity(_published, Ers, SupportedFeatures.Assignment, true, now.AddDays(7)));
        _activities.AddActivity(new Activity(_draft, Ers, SupportedFeatures.Assignment, false, now.AddDays(7)));
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test] 
    public async Task Pocetna_StudentDobijaVezuKaPredmetuIKaObjavljenomZadatku_ANeKaNacrtu()
    {
        using var client = ClientFor(DemoData.DemoStudentId);

        var json = await client.GetStringAsync("/pocetna");
        using var doc = JsonDocument.Parse(json);
        var ers = doc.RootElement.GetProperty("enrolled").EnumerateArray()
            .Single(card => card.GetProperty("offeringId").GetGuid() == Ers);

        var links = ers.GetProperty("links").EnumerateArray()
            .Select(l => l.GetProperty("href").GetString()).ToArray();
        var tasks = ers.GetProperty("openTasks").EnumerateArray()
            .Select(t => t.GetProperty("href").GetString()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(links, Does.Contain($"/offerings/{Ers}"));
            Assert.That(tasks, Is.EqualTo(new[] { $"/offerings/{Ers}/assignments/{_published}" }));
            Assert.That(json, Does.Not.Contain(_draft.ToString()));
        });
    }

    [Test]
    public async Task OtvaranjeVeze_UpisanStudent_OtvaraPredmetIObjavljenZadatak()
    {
        using var client = ClientFor(DemoData.DemoStudentId);

        var offering = await client.GetAsync($"/offerings/{Ers}");
        var assignment = await client.GetAsync($"/offerings/{Ers}/assignments/{_published}");

        Assert.Multiple(() =>
        {
            Assert.That(offering.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(assignment.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test] 
    public async Task OtvaranjeVeze_StudentNeMozeDaOtvoriNacrtZadatka()
    {
        using var client = ClientFor(DemoData.DemoStudentId);

        var response = await client.GetAsync($"/offerings/{Ers}/assignments/{_draft}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test] 
    public async Task OtvaranjeVeze_NastavnikOtvaraNacrtNaSvomIzvodjenju()
    {
        using var client = ClientFor(DemoData.DemoTeacherId);

        var response = await client.GetAsync($"/offerings/{Ers}/assignments/{_draft}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test] 
    public async Task OtvaranjeVeze_KorisnikBezClanstva_Dobija404()
    {
        using var client = ClientFor(Guid.NewGuid());

        var offering = await client.GetAsync($"/offerings/{Ers}");
        var assignment = await client.GetAsync($"/offerings/{Ers}/assignments/{_published}");

        Assert.Multiple(() =>
        {
            Assert.That(offering.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(assignment.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test] 
    public async Task OtvaranjeVeze_UklonjenaAsistentkinja_Dobija404()
    {
        using var client = ClientFor(DemoData.DemoAssistantId);

        var response = await client.GetAsync($"/offerings/{Ers}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("/offerings/33333333-0000-0000-0000-000000000001")]
    [TestCase("/offerings/33333333-0000-0000-0000-000000000001/assignments/66666666-0000-0000-0000-000000000001")]
    public async Task OtvaranjeVeze_BezSesije_Vraca401(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private HttpClient ClientFor(Guid userId)
    {
        var token = Guid.NewGuid().ToString("N");
        _sessions.Register(token, userId);
        _accounts.Activate(userId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
