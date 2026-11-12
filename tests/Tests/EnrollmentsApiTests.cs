using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Application.DTOs;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-UserId";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

[TestFixture]
public class EnrollmentsApiTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private static string EnrollmentsUrl(Guid offeringId) => $"/api/offerings/{offeringId}/enrollments";

    [SetUp]
    public void SetUp()
    {
        // Nova fabrika po testu, da PATCH iz jednog testa ne utiče na drugi.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private void LoginAs(Guid teacherId)
    {
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, teacherId.ToString());
    }

    [Test]
    public async Task Get_ReturnsActiveEnrollments_ForAssignedTeacher()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.ErsId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<StudentEnrollmentDto>>();
        Assert.That(body, Has.Count.EqualTo(2), "Prekinuto članstvo ne sme biti na spisku.");
    }

    [Test]
    public async Task Get_ReturnsForbidden_ForTeacherWithoutAccess()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.OibId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Get_ReturnsUnauthorized_WithoutLogin()
    {
        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.ErsId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Patch_TerminatesEnrollment_AndRemovesItFromList()
    {
        LoginAs(DemoData.ErsTeacherId);
        var enrollmentId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

        var patch = await _client.PatchAsync($"{EnrollmentsUrl(DemoData.ErsId)}/{enrollmentId}/terminate", null);
        Assert.That(patch.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var list = await _client.GetFromJsonAsync<List<StudentEnrollmentDto>>(EnrollmentsUrl(DemoData.ErsId));
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list!.Exists(e => e.EnrollmentId == enrollmentId), Is.False);
    }

    [Test]
    public async Task Patch_ReturnsForbidden_ForTeacherWithoutAccess()
    {
        LoginAs(Guid.NewGuid());
        var enrollmentId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

        var response = await _client.PatchAsync($"{EnrollmentsUrl(DemoData.ErsId)}/{enrollmentId}/terminate", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Patch_ReturnsNotFound_ForUnknownEnrollment()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await _client.PatchAsync($"{EnrollmentsUrl(DemoData.ErsId)}/{Guid.NewGuid()}/terminate", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
