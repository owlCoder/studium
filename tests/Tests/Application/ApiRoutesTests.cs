using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public class ApiRoutesTests
{
    private WebApplicationFactory<Program> _factory;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
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

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Ruta /health mora biti dostupna.");
    }

    [Test]
    public async Task GetDemoCoursesRoute_ReturnsOk()
    {

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/demo/courses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Postojeća ruta /demo/courses mora ostati dostupna.");
    }
}
