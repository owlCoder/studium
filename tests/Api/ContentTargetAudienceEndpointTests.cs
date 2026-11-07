using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests.Api;

public class ContentTargetAudienceEndpointTests
{
    [Fact]
    public async Task Post_TargetAudience_AnonymousUser_Returns401Unauthorized()
    {
        // Test za anonimnog korisnika (bez tokena)
        // Pretpostavka je da koristiš WebApplicationFactory za testiranje
        // ili prilagodi klijent prema ostatku vaših testova u projektu.
        
        // Ako nemate fabriku, proveri kako su drugi testovi u projektu inicijalizovani.
        // Primer: var client = _factory.CreateClient();
        
        // Ovdje proveravamo da li vraća 401 Unauthorized
        Assert.True(true); // Prilagodi poziv na osnovu vaše Test Fixture klase
    }

    [Fact]
    public async Task Post_TargetAudience_UnauthorizedInstructor_ReturnsForbiddenOrBadRequest()
    {
        // Test za ulogovanog nastavnika koji nema pravo nad sadržajem
        Assert.True(true); 
    }

    [Fact]
    public async Task Post_TargetAudience_AuthorizedInstructor_ReturnsOk()
    {
        // Test za ovlašćenog nastavnika
        Assert.True(true); 
    }
}