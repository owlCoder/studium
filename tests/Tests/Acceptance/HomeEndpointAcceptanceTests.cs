using Stopwatch = System.Diagnostics.Stopwatch;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Activities;
using Application.Enrollments;
using Application.Home;
using Domain.Activities;
using Infrastructure.Dashboard;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Acceptance;

[TestFixture]
[Category("Acceptance")]
public sealed class HomeEndpointAcceptanceTests
{
    private St007Scenario _scenario = null!;
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _scenario = new St007Scenario();
    }

    [TearDown]
    public void TearDown()
    {
        _factory?.Dispose();
    }

    private void StartApi()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IStudentOfferingsReader>(_ => _scenario.CreateStudentReader());
                    services.AddSingleton<IActivityReader>(_scenario.Activities);
                    services.AddSingleton<IActivitySubmissionReader>(_scenario.Activities);
                });
            });
    }

    private HttpClient LoggedInStudent()
    {
        var token = Guid.NewGuid().ToString("N");
        _factory.Services.GetRequiredService<InMemorySessionReader>().Register(token, St007Scenario.StudentId);
        _factory.Services.GetRequiredService<InMemoryAccountStatusReader>().Activate(St007Scenario.StudentId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<HomeOverviewDto> GetHome(HttpClient client)
    {
        var response = await client.GetAsync("/pocetna");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return (await response.Content.ReadFromJsonAsync<HomeOverviewDto>())!;
    }

    [Test]
    public async Task Http_StudentIzDvaPredmeta_VidiObaPredmetaIBrojOtvorenihZadataka()
    {
        var ers = _scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        var oib = _scenario.AddEnrolledOffering("OIB", "Osnove informacione bezbednosti");
        _scenario.Activities.Add(new Activity(Guid.NewGuid(), ers, SupportedFeatures.Assignment, true,
            DateTimeOffset.UtcNow.AddDays(7)));
        StartApi();
        using var client = LoggedInStudent();

        var home = await GetHome(client);

        Assert.Multiple(() =>
        {
            Assert.That(home.Enrolled.Select(x => x.OfferingId), Is.EquivalentTo(new[] { ers, oib }));
            Assert.That(home.Enrolled.Single(x => x.OfferingId == ers).OpenActivities, Is.EqualTo(1));
            Assert.That(home.Enrolled.Single(x => x.OfferingId == oib).OpenActivities, Is.EqualTo(0));
            Assert.That(home.Enrolled.Single(x => x.OfferingId == ers).Links.Select(l => l.Href),
                Does.Contain($"/offerings/{ers}/assignments"));
        });
    }

    [Test]
    public async Task Http_PromenaRoka_VidiSeVecPriSledecemZahtevu()
    {
        var ers = _scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        var taskId = Guid.NewGuid();
        _scenario.Activities.Add(new Activity(taskId, ers, SupportedFeatures.Assignment, true,
            DateTimeOffset.UtcNow.AddDays(2)));
        StartApi();
        using var client = LoggedInStudent();

        var before = await GetHome(client);
        _scenario.Activities.ChangeDeadline(taskId, DateTimeOffset.UtcNow.AddMinutes(-5));
        var after = await GetHome(client);

        Assert.Multiple(() =>
        {
            Assert.That(before.Enrolled.Single().OpenActivities, Is.EqualTo(1));
            Assert.That(after.Enrolled.Single().OpenActivities, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Http_PocetnaSeNeKesira() 
    {
        _scenario.AddEnrolledOffering("ERS", "Elementi razvoja softvera");
        StartApi();
        using var client = LoggedInStudent();

        var response = await client.GetAsync("/pocetna");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.CacheControl?.MaxAge, Is.Null);
            Assert.That(response.Headers.CacheControl?.Public ?? false, Is.False);
            Assert.That(response.Headers.ETag, Is.Null);
            Assert.That(response.Content.Headers.Expires, Is.Null);
            Assert.That(response.Content.Headers.LastModified, Is.Null);
        });
    }

    [Test]
    public async Task Http_VremeOdgovora_ZaKorisnikaSa10Izvodjenja()
    {
        var offerings = Enumerable.Range(1, 10)
            .Select(i => _scenario.AddEnrolledOffering($"P{i:00}", $"Predmet {i:00}"))
            .ToArray();
        foreach (var offeringId in offerings)
        {
            for (var t = 0; t < 5; t++)
            {
                _scenario.Activities.Add(new Activity(Guid.NewGuid(), offeringId, SupportedFeatures.Assignment, true,
                    DateTimeOffset.UtcNow.AddDays(t + 1)));
            }
        }
        StartApi();
        using var client = LoggedInStudent();

        // Zagrevanje: prvi zahtev uključuje pokretanje i JIT, pa se ne meri.
        var warmUp = await GetHome(client);
        Assert.That(warmUp.Enrolled, Has.Count.EqualTo(10));

        const int runs = 20;
        var timings = new List<double>(runs);
        for (var i = 0; i < runs; i++)
        {
            var watch = Stopwatch.StartNew();
            var response = await client.GetAsync("/pocetna");
            await response.Content.ReadAsByteArrayAsync();
            watch.Stop();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            timings.Add(watch.Elapsed.TotalMilliseconds);
        }

        timings.Sort();
        var median = timings[runs / 2];
        var p95 = timings[(int)Math.Ceiling(runs * 0.95) - 1];
        var max = timings[^1];

        TestContext.Out.WriteLine(
            $"US-7.12 | GET /pocetna, 10 izvođenja, 50 aktivnosti, {runs} merenja: " +
            $"medijana {median:F1} ms, p95 {p95:F1} ms, max {max:F1} ms");
    }
}
