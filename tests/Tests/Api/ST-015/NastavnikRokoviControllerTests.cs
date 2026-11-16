using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Studium.Api.Controllers;
using Studium.Domain.ST015.Entities;
using Studium.Domain.ST015.Repositories;

namespace Studium.Tests.Api.ST015;

[TestFixture]
public class NastavnikRokoviControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private Mock<IRokoviRepository> _mockRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IRokoviRepository>();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRokoviRepository>();
                services.AddSingleton(_mockRepo.Object);

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task ProduziPojedinacniRok_AnonimniZahtev_Vraca401Unauthorized()
    {
        var client = _factory.CreateClient();
        var request = new ExtendIndividualDeadlineRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 11, 20));

        var response = await client.PutAsJsonAsync("/api/nastavnik/rokovi/pojedinacni", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        _mockRepo.Verify(r => r.UpdateStudentRokAsync(It.IsAny<StudentZadatakRok>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProduziPojedinacniRok_NeovlasceniKorisnik_Vraca403ForbiddenINeMenjaStanje()
    {
        var neovlasceniKorisnikId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();

        _mockRepo.Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(neovlasceniKorisnikId, zadatakId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, neovlasceniKorisnikId.ToString());

        var request = new ExtendIndividualDeadlineRequest(studentId, zadatakId, new DateTime(2026, 11, 20));

        var response = await client.PutAsJsonAsync("/api/nastavnik/rokovi/pojedinacni", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        _mockRepo.Verify(r => r.UpdateStudentRokAsync(It.IsAny<StudentZadatakRok>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProduziPojedinacniRok_ValidanNastavnik_Vraca200OkIAzuriraRok()
    {
        var nastavnikId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();
        var noviRok = new DateTime(2026, 11, 20);

        _mockRepo.Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(nastavnikId, zadatakId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var studentRok = new StudentZadatakRok(studentId, zadatakId);
        _mockRepo.Setup(r => r.GetStudentRokAsync(studentId, zadatakId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(studentRok);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, nastavnikId.ToString());

        var request = new ExtendIndividualDeadlineRequest(studentId, zadatakId, noviRok);

        var response = await client.PutAsJsonAsync("/api/nastavnik/rokovi/pojedinacni", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(studentRok.PojedinacniRok, Is.EqualTo(noviRok));
        _mockRepo.Verify(r => r.UpdateStudentRokAsync(studentRok, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProduziPojedinacniRok_ProdukcioniCompositionRoot_BezDiGreskeVraca403Forbidden()
    {
        await using var prodFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });

        var client = prodFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.SchemeName, Guid.NewGuid().ToString());

        var request = new ExtendIndividualDeadlineRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 11, 20));

        var response = await client.PutAsJsonAsync("/api/nastavnik/rokovi/pojedinacni", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestScheme";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return Task.FromResult(AuthenticateResult.NoResult());

            var headerValue = authHeader.ToString();
            if (!headerValue.StartsWith($"{SchemeName} ", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(AuthenticateResult.Fail("Neispravna šema."));

            var userId = headerValue[$"{SchemeName} ".Length..].Trim();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Nastavnik")
            };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}