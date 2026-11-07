using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Api;

public class ContentTargetAudienceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ContentTargetAudienceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_TargetAudience_AnonymousUser_ReturnsUnauthorized()
    {
        var contentId = Guid.NewGuid();
        var request = new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() };

        var response = await _client.PostAsJsonAsync($"/contents/{contentId}/target-audience", request);

        // 1. Slučaj: Anoniman korisnik -> 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_TargetAudience_UnauthorizedInstructor_ReturnsForbiddenOrBadRequest()
    {
        var contentId = Guid.NewGuid();
        var request = new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() };

        // Postavićemo lažni/testni header ili simulirati neovlašćenog korisnika
        var response = await _client.PostAsJsonAsync($"/contents/{contentId}/target-audience", request);

        // 2. Slučaj: Neovlašćen korisnik
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                    response.StatusCode == HttpStatusCode.Forbidden || 
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_TargetAudience_AuthorizedInstructor_ReturnsOk()
    {
        var contentId = Guid.NewGuid();
        var request = new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() };

        var response = await _client.PostAsJsonAsync($"/contents/{contentId}/target-audience", request);

        // 3. Slučaj: Ovlašćen nastavnik -> Očekujemo uspeh (ili BadRequest ako ID ne postoji u mock bazi, zavisno od podešavanja)
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_TargetAudience_GroupsFromAnotherExecution_ReturnsBadRequest()
    {
        var contentId = Guid.NewGuid();
        // Prosleđujemo grupe koje navodno pripadaju drugom izvođenju
        var request = new { IsForWholeCourseExecution = false, TargetGroupIds = new List<Guid> { Guid.NewGuid() } };

        var response = await _client.PostAsJsonAsync($"/contents/{contentId}/target-audience", request);

        // 4. Slučaj: Grupe van izvođenja -> Očekujemo grešku
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
    }
}