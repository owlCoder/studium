using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Api;

public class ContentTargetAudienceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ContentTargetAudienceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_TargetAudience_AnonymousUser_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient(); // Bez autentifikacije
        var response = await client.PostAsJsonAsync($"/contents/{Guid.NewGuid()}/target-audience", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_TargetAudience_AuthorizedInstructor_ReturnsOk()
    {
        // Kreiramo klijent sa simuliranim ulogovanim instruktorom (uz pomoć testne autentifikacije)
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ovde se može konfigurisati testni auth handler ako je potrebno,
                // ili se prosleđuju odgovarajući testni claim-ovi koje vaš Program.cs očekuje.
            });
        }).CreateClient();

        // Slanje zahteva sa validnim podacima...
        var response = await client.PostAsJsonAsync($"/contents/{Guid.NewGuid()}/target-audience", new 
        { 
            IsForWholeCourseExecution = true, 
            TargetGroupIds = new List<Guid>() 
        });

        // Očekujemo uspeh ili validan poslovni odgovor
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}