using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests.Integration;

[TestFixture]
public sealed class AccountFlowTests
{
    private const string Username = "Ana.Petrovic";
    private const string DemoPassword = "Demo-Lozinka-1";
    private const string NewPassword = "Nova-Lozinka-2";

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

    private async Task<HttpResponseMessage> LoginAsync(string password)
        => await _client.PostAsJsonAsync("/api/auth/login",
            new { username = Username, password });

    private async Task<string> LoginAndGetTokenAsync(string password = DemoPassword)
    {
        var response = await LoginAsync(password);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static HttpRequestMessage Post(string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    private static HttpRequestMessage Get(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Test]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await _client.SendAsync(Post("/api/auth/change-password", null,
            new { currentPassword = DemoPassword, newPassword = NewPassword }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await _client.SendAsync(Post("/api/auth/logout", null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_ThenChangePassword_NewPasswordWorksAndOldOneIsRejected()
    {
        var token = await LoginAndGetTokenAsync();

        var change = await _client.SendAsync(Post("/api/auth/change-password", token,
            new { currentPassword = DemoPassword, newPassword = NewPassword }));
        Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var withOld = await LoginAsync(DemoPassword);
        var withNew = await LoginAsync(NewPassword);

        Assert.That(withOld.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(withNew.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400_AndKeepsOldPassword()
    {
        var token = await LoginAndGetTokenAsync();

        var change = await _client.SendAsync(Post("/api/auth/change-password", token,
            new { currentPassword = "Pogresna-Lozinka-9", newPassword = NewPassword }));
        Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var stillOld = await LoginAsync(DemoPassword);
        Assert.That(stillOld.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_ThenLogout_OldSessionIsRejected()
    {
        var token = await LoginAndGetTokenAsync();

        var before = await _client.SendAsync(Get("/api/auth/session", token));
        Assert.That(before.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var logout = await _client.SendAsync(Post("/api/auth/logout", token));
        Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var session = await _client.SendAsync(Get("/api/auth/session", token));
        var change = await _client.SendAsync(Post("/api/auth/change-password", token,
            new { currentPassword = DemoPassword, newPassword = NewPassword }));

        Assert.That(session.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_ThenLoginAgain_NewSessionIsAccepted()
    {
        var oldToken = await LoginAndGetTokenAsync();
        await _client.SendAsync(Post("/api/auth/logout", oldToken));

        // Mala pauza, da nova sesija sigurno nastane posle trenutka odjave.
        await Task.Delay(50);

        var newToken = await LoginAndGetTokenAsync();
        var session = await _client.SendAsync(Get("/api/auth/session", newToken));
        var oldSession = await _client.SendAsync(Get("/api/auth/session", oldToken));

        Assert.That(session.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(oldSession.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
