using System.Net;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// Swagger (UI i JSON) je razvojni alat: sme da se izlaze samo u Development okruzenju.
/// </summary>
[TestFixture]
public class SwaggerExposureTests
{
    [TestCase("Development", HttpStatusCode.OK)]
    [TestCase("Testing", HttpStatusCode.NotFound)]
    [TestCase("Staging", HttpStatusCode.NotFound)]
    [TestCase("Production", HttpStatusCode.NotFound)]
    public async Task SwaggerUi_IsServedOnlyInDevelopment(string environment, HttpStatusCode expected)
    {
        using var factory = ApiTestHost.CreateWithApplicationAuthentication(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }

    [TestCase("Testing")]
    [TestCase("Staging")]
    [TestCase("Production")]
    public async Task SwaggerJson_IsNotServedOutsideDevelopment(string environment)
    {
        using var factory = ApiTestHost.CreateWithApplicationAuthentication(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}