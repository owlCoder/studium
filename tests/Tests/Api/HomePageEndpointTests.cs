using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// US-7.7: početna strana sa karticama izvođenja, nad pravim Api pipeline-om.
/// Proverava da stranica i njeni fajlovi stižu bez podataka, a da kartice dobijaju
/// naziv predmeta i akademsku godinu samo preko zaštićenog GET /pocetna.
/// </summary>
[TestFixture]
public sealed class HomePageEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private InMemorySessionReader _sessions = null!;
    private InMemoryAccountStatusReader _accounts = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        _sessions = _factory.Services.GetRequiredService<InMemorySessionReader>();
        _accounts = _factory.Services.GetRequiredService<InMemoryAccountStatusReader>();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task PocetnaStrana_DostupnaBezSesije_INeSadrziPodatkeOPredmetima()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(html, Does.Contain("id=\"pocetna\""));
            Assert.That(html, Does.Contain("js/pocetna.js"));
            Assert.That(html, Does.Not.Contain("Elementi razvoja softvera"));
            Assert.That(html, Does.Not.Contain("Osnove informacione bezbednosti"));
        });
    }

    [TestCase("/js/offering-card.js", "javascript")]
    [TestCase("/js/home-api.js", "javascript")]
    [TestCase("/js/pocetna.js", "javascript")]
    [TestCase("/css/pocetna.css", "text/css")]
    public async Task FajloviKartice_SeServirajuSaIspravnimTipom(string url, string expectedMediaType)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Does.Contain(expectedMediaType));
        });
    }

    [Test]
    public async Task PrikazZaUzakEkran_StranicaImaViewport_AStiloviJednuKolonu()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var css = await client.GetStringAsync("/css/pocetna.css");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("name=\"viewport\""));
            Assert.That(html, Does.Contain("width=device-width"));
            Assert.That(css, Does.Contain("@media (max-width: 30rem)"));
            Assert.That(css, Does.Contain("grid-template-columns: 1fr"));
        });
    }

    [Test] // Pozitivan scenario: demo student vidi oba upisana predmeta sa nazivom i godinom.
    public async Task Pocetna_UpisanStudent_DobijaNazivIGodinuZaSvakuKarticu()
    {
        using var client = ClientFor(DemoData.DemoStudentId);

        var response = await client.GetAsync("/pocetna");
        var cards = await ReadEnrolledAsync(response);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(cards.Select(card => card.CourseName), Is.EquivalentTo(new[]
            {
                "Elementi razvoja softvera",
                "Osnove informacione bezbednosti"
            }));
            Assert.That(cards.Select(card => card.AcademicYear), Has.All.EqualTo("2026/27"));
            Assert.That(cards.Select(card => card.OfferingId), Has.None.EqualTo(Guid.Empty));
        });
    }

    [Test] // Negativan scenario: prekinuto članstvo (ERS 2025/26) ne dobija karticu.
    public async Task Pocetna_PrekinutoClanstvo_NemaKarticu()
    {
        using var client = ClientFor(DemoData.DemoStudentId);

        var cards = await ReadEnrolledAsync(await client.GetAsync("/pocetna"));

        Assert.That(cards.Select(card => card.AcademicYear), Has.None.EqualTo("2025/26"));
    }

    [Test] // Negativan scenario: drugi student ne vidi tuđe predmete.
    public async Task Pocetna_StudentBezUpisa_DobijaPraznuListuKartica()
    {
        using var client = ClientFor(Guid.NewGuid());

        var response = await client.GetAsync("/pocetna");
        var cards = await ReadEnrolledAsync(response);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(cards, Is.Empty);
        });
    }

    [Test] // Negativan scenario: ID iz zahteva ne mijenja čije se kartice vraćaju.
    public async Task Pocetna_IdDrugogStudentaUZahtevu_SeIgnorise()
    {
        using var client = ClientFor(Guid.NewGuid());

        var response = await client.GetAsync($"/pocetna?studentId={DemoData.DemoStudentId}");
        var cards = await ReadEnrolledAsync(response);

        Assert.That(cards, Is.Empty);
    }

    [Test] // Negativan scenario: bez sesije nema podataka za kartice.
    public async Task Pocetna_BezSesije_Vraca401BezPodataka()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/pocetna");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(body, Does.Not.Contain("Elementi razvoja softvera"));
        });
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

    // Čita JSON po imenima polja koja koristi offering-card.js (camelCase),
    // pa test pada ako se ugovor koji kartica očekuje promijeni.
    private static async Task<IReadOnlyList<CardData>> ReadEnrolledAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return Array.Empty<CardData>();
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("enrolled")
            .EnumerateArray()
            .Select(item => new CardData(
                item.GetProperty("offeringId").GetGuid(),
                item.GetProperty("courseName").GetString()!,
                item.GetProperty("academicYear").GetString()!))
            .ToArray();
    }

    private sealed record CardData(Guid OfferingId, string CourseName, string AcademicYear);
}
