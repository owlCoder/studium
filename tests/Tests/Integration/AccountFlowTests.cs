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

    private async Task<HttpStatusCode> SessionStatusAsync(string token)
        => (await _client.SendAsync(Get("/api/auth/session", token))).StatusCode;

    private async Task<HttpStatusCode> ChangePasswordAsync(string token, string current, string next)
        => (await _client.SendAsync(Post("/api/auth/change-password", token,
            new { currentPassword = current, newPassword = next }))).StatusCode;

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

        Assert.That(await ChangePasswordAsync(token, DemoPassword, NewPassword),
            Is.EqualTo(HttpStatusCode.NoContent));

        var withOld = await LoginAsync(DemoPassword);
        var withNew = await LoginAsync(NewPassword);

        Assert.That(withOld.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(withNew.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400_AndKeepsOldPassword()
    {
        var token = await LoginAndGetTokenAsync();

        Assert.That(await ChangePasswordAsync(token, "Pogresna-Lozinka-9", NewPassword),
            Is.EqualTo(HttpStatusCode.BadRequest));

        var stillOld = await LoginAsync(DemoPassword);
        Assert.That(stillOld.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_ThenLogout_OldSessionIsRejected()
    {
        var token = await LoginAndGetTokenAsync();
        Assert.That(await SessionStatusAsync(token), Is.EqualTo(HttpStatusCode.OK));

        var logout = await _client.SendAsync(Post("/api/auth/logout", token));
        Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        Assert.That(await SessionStatusAsync(token), Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await ChangePasswordAsync(token, DemoPassword, NewPassword),
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_OnOneDevice_DoesNotAffectTheOtherDevice()
    {
        var phone = await LoginAndGetTokenAsync();
        var laptop = await LoginAndGetTokenAsync();

        var logout = await _client.SendAsync(Post("/api/auth/logout", phone));
        Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        Assert.That(await SessionStatusAsync(phone), Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await SessionStatusAsync(laptop), Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Logout_TwiceWithTheSameToken_SecondAttemptIsRejected()
    {
        var token = await LoginAndGetTokenAsync();

        var first = await _client.SendAsync(Post("/api/auth/logout", token));
        var second = await _client.SendAsync(Post("/api/auth/logout", token));

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ChangePassword_RevokesTheOtherDevice_AndKeepsTheCurrentOne()
    {
        var phone = await LoginAndGetTokenAsync();
        var laptop = await LoginAndGetTokenAsync();

        Assert.That(await ChangePasswordAsync(phone, DemoPassword, NewPassword),
            Is.EqualTo(HttpStatusCode.NoContent));

        // Token izdat pre promene lozinke, sa drugog uređaja, više ne važi.
        Assert.That(await SessionStatusAsync(laptop), Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await ChangePasswordAsync(laptop, NewPassword, "Treca-Lozinka-3"),
            Is.EqualTo(HttpStatusCode.Unauthorized));

        // Sesija sa koje je lozinka promenjena ostaje važeća.
        Assert.That(await SessionStatusAsync(phone), Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ChangePassword_WithWrongCurrentPassword_DoesNotRevokeTheOtherDevice()
    {
        var phone = await LoginAndGetTokenAsync();
        var laptop = await LoginAndGetTokenAsync();

        Assert.That(await ChangePasswordAsync(phone, "Pogresna-Lozinka-9", NewPassword),
            Is.EqualTo(HttpStatusCode.BadRequest));

        Assert.That(await SessionStatusAsync(phone), Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await SessionStatusAsync(laptop), Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ChangePassword_ThenLoginAgain_NewSessionIsAccepted()
    {
        var oldToken = await LoginAndGetTokenAsync();
        var current = await LoginAndGetTokenAsync();

        await ChangePasswordAsync(current, DemoPassword, NewPassword);

        // Mala pauza, da nova sesija sigurno nastane posle trenutka promene lozinke.
        await Task.Delay(50);

        var newToken = await LoginAndGetTokenAsync(NewPassword);

        Assert.That(await SessionStatusAsync(newToken), Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await SessionStatusAsync(oldToken), Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_ThenLoginAgain_NewSessionIsAccepted()
    {
        var oldToken = await LoginAndGetTokenAsync();
        await _client.SendAsync(Post("/api/auth/logout", oldToken));

        var newToken = await LoginAndGetTokenAsync();

        Assert.That(await SessionStatusAsync(newToken), Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await SessionStatusAsync(oldToken), Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
