using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// US-7.5: HTTP testovi identiteta iz sesije nad pravim Api pipeline-om.
/// Tokeni su sintetički i generišu se u testu.
/// </summary>
[TestFixture]
public sealed class SessionIdentityEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private InMemorySessionReader _sessions = null!;
    private InMemoryAccountStatusReader _accounts = null!;

    [SetUp]
    public void SetUp()
    {
        // "Testing" isključuje validaciju DI grafa pri pokretanju, pa test ne zavisi
        // od toga da li su servisi ostalih storyja (US-7.1–7.4) već registrovani.
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

    [TestCase("/api/ja")]
    [TestCase("/api/student/izvodjenja")]
    [TestCase("/api/moja-izvodjenja")]
    public async Task BezSesije_Vraca401(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task NepoznatToken_Vraca401()
    {
        using var client = ClientWithToken(NewToken());

        var response = await client.GetAsync("/api/ja");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TokenUQueryStringu_SeNePrihvata()
    {
        var token = NewToken();
        var userId = Guid.NewGuid();
        _sessions.Register(token, userId);
        _accounts.Activate(userId);
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/ja?token={token}&access_token={token}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task VazecaSesija_AktivanNalog_VracaIdIzSesije()
    {
        var token = NewToken();
        var userId = Guid.NewGuid();
        _sessions.Register(token, userId);
        _accounts.Activate(userId);
        using var client = ClientWithToken(token);

        var response = await client.GetAsync("/api/ja");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<WhoAmI>();
        Assert.That(body!.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task IdIzZahteva_SeIgnorise_KoristiSeSamoIdIzSesije()
    {
        var token = NewToken();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _sessions.Register(token, userId);
        _accounts.Activate(userId);
        _accounts.Activate(otherUserId);
        using var client = ClientWithToken(token);
        client.DefaultRequestHeaders.Add("X-User-Id", otherUserId.ToString());

        var response = await client.GetAsync($"/api/ja?userId={otherUserId}&studentId={otherUserId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<WhoAmI>();
        Assert.That(body!.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task DeaktiviranNalog_SledeciZahtevDobijaOdbijenPristup()
    {
        var token = NewToken();
        var userId = Guid.NewGuid();
        _sessions.Register(token, userId);
        _accounts.Activate(userId);
        using var client = ClientWithToken(token);

        var beforeDeactivation = await client.GetAsync("/api/ja");
        _accounts.Deactivate(userId);
        var afterDeactivation = await client.GetAsync("/api/ja");
        var homeAfterDeactivation = await client.GetAsync("/api/moja-izvodjenja");
        Assert.That(beforeDeactivation.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(afterDeactivation.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(homeAfterDeactivation.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OpozvanaSesija_SledeciZahtevDobijaOdbijenPristup()
    {
        var token = NewToken();
        var userId = Guid.NewGuid();
        _sessions.Register(token, userId);
        _accounts.Activate(userId);
        using var client = ClientWithToken(token);

        var before = await client.GetAsync("/api/ja");
        _sessions.Revoke(token);
        var after = await client.GetAsync("/api/ja");

        Assert.That(before.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(after.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private HttpClient ClientWithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string NewToken() => Guid.NewGuid().ToString("N");

    private sealed record WhoAmI(Guid UserId);
}
