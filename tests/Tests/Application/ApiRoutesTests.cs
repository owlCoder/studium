using System;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Tests.Application;

// Pomoćna klasa za presretanje autentifikacije u testovima
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("Test-UserId", out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Nema autorizacije"));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userIdValues.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

[TestFixture]
public class ApiRoutesTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _authorizedStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Student sa grupom
    private readonly Guid _noGroupStudentId = Guid.Parse("33333333-3333-3333-3333-333333333333"); // Student bez grupe

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<Microsoft.AspNetCore.Authentication.ISystemClock, Microsoft.AspNetCore.Authentication.SystemClock>();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "MockScheme";
                    options.DefaultChallengeScheme = "MockScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("MockScheme", options => { });
            });
        });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task GetHealthRoute_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetDemoCoursesRoute_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/demo/courses");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // Anonimni poziv (korisnik nije ulogovan)
    [Test]
    public async Task GetContent_AnonymousCall_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/courses/{_courseId}/content?studentId={_authorizedStudentId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    //Pokušaj čitanja za drugog studenta
    [Test]
    public async Task GetContent_ReadingForAnotherStudent_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-UserId", _noGroupStudentId.ToString());

        var response = await client.GetAsync($"/api/courses/{_courseId}/content?studentId={_authorizedStudentId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    //Korisnik bez članstva u grupi (Uspešno)
    [Test]
    public async Task GetContent_StudentWithoutMembership_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-UserId", _noGroupStudentId.ToString());

        var response = await client.GetAsync($"/api/courses/{_courseId}/content?studentId={_noGroupStudentId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    //Pozitivan slučaj ovlašćenog studenta (Uspešno)
    [Test]
    public async Task GetContent_AuthorizedStudent_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-UserId", _authorizedStudentId.ToString());

        var response = await client.GetAsync($"/api/courses/{_courseId}/content?studentId={_authorizedStudentId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
