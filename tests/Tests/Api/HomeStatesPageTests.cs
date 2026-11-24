using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests.Api;

[TestFixture]
public sealed class HomeStatesPageTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [TestCase("/js/home-status.js")]
    [TestCase("/js/home-page.js")]
    [TestCase("/js/date-format.js")]
    public async Task SkripteStanja_SeServirajuKaoJavaScript(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Does.Contain("javascript"));
        });
    }

    [Test]
    public async Task PocetnaStrana_ImaMestoZaPorukuStanjaIVremeUcitavanja()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("id=\"home-status\""));
            Assert.That(html, Does.Contain("aria-live=\"polite\""));
            Assert.That(html, Does.Contain("id=\"home-updated\""));
        });
    }

    [Test]
    public async Task Stilovi_ImajuStanjaUcitavanjaPraznoIGreska()
    {
        using var client = _factory.CreateClient();

        var css = await client.GetStringAsync("/css/pocetna.css");

        Assert.Multiple(() =>
        {
            Assert.That(css, Does.Contain(".home-status[data-state=\"loading\"]"));
            Assert.That(css, Does.Contain(".home-status[data-state=\"empty\"]"));
            Assert.That(css, Does.Contain(".home-status[data-state=\"error\"]"));
            Assert.That(css, Does.Contain(".home-status__retry"));
        });
    }
}
