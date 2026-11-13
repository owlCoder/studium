using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Studium.Domain.ST015.Entities;
using Studium.Domain.ST015.Repositories;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Studium.Tests.Api.ST015;

[TestFixture]
public class StudentRokoviControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private Mock<IRokoviRepository> _rokoviRepoMock = null!;

    private readonly Guid _zadatakId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _drugiStudentId = Guid.NewGuid();
    private readonly Guid _validanNastavnikId = Guid.NewGuid();
    private readonly Guid _neovlascenNastavnikId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _rokoviRepoMock = new Mock<IRokoviRepository>();

        var zadatak = new Zadatak(_zadatakId, DateTime.UtcNow.AddDays(5));
        _rokoviRepoMock
            .Setup(r => r.GetZadatakAsync(_zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zadatak);

        _rokoviRepoMock
            .Setup(r => r.GetStudentRokAsync(_studentId, _zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentZadatakRok?)null);

        _rokoviRepoMock
            .Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(_validanNastavnikId, _zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _rokoviRepoMock
            .Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(_neovlascenNastavnikId, _zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_rokoviRepoMock.Object);
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
            });
        });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    [Test]
    public async Task GetPrimenljiviRok_AnonimniZahtev_Vraca401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/StudentRokovi/zadatci/{_zadatakId}/studenti/{_studentId}/primenljivi-rok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetPrimenljiviRok_DrugiStudent_Vraca403Forbidden()
    {
        var client = CreateAuthenticatedClient(_drugiStudentId, "Student");

        var response = await client.GetAsync($"/api/StudentRokovi/zadatci/{_zadatakId}/studenti/{_studentId}/primenljivi-rok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetPrimenljiviRok_NeovlascenNastavnik_Vraca403Forbidden()
    {
        var client = CreateAuthenticatedClient(_neovlascenNastavnikId, "Nastavnik");

        var response = await client.GetAsync($"/api/StudentRokovi/zadatci/{_zadatakId}/studenti/{_studentId}/primenljivi-rok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetPrimenljiviRok_ValidanNastavnik_Vraca200Ok()
    {
        var client = CreateAuthenticatedClient(_validanNastavnikId, "Nastavnik");

        var response = await client.GetAsync($"/api/StudentRokovi/zadatci/{_zadatakId}/studenti/{_studentId}/primenljivi-rok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetPrimenljiviRok_IstiStudent_Vraca200Ok()
    {
        var client = CreateAuthenticatedClient(_studentId, "Student");

        var response = await client.GetAsync($"/api/StudentRokovi/zadatci/{_zadatakId}/studenti/{_studentId}/primenljivi-rok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Request.Headers["X-Test-UserId"].ToString();
        var role = Request.Headers["X-Test-Role"].ToString();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}