using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.ContentAudience;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Tests.Api;

// Test autentifikacija: ulogovani korisnik se šalje preko headera X-Test-User
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var id))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Kontrolisani podaci umesto hardkodovanih sentinel GUID-ova
public sealed class FakeAudienceLookup : IContentLookupForAudience, IGroupLookupForAudience
{
    public Dictionary<Guid, Guid> ContentToExecution { get; } = new();
    public HashSet<(Guid Content, Guid Instructor)> Editors { get; } = new();
    public Dictionary<Guid, Guid> GroupToExecution { get; } = new();

    public Task<bool> ExistsAsync(Guid contentId, CancellationToken ct) =>
        Task.FromResult(ContentToExecution.ContainsKey(contentId));

    public Task<Guid> GetCourseExecutionIdAsync(Guid contentId, CancellationToken ct) =>
        Task.FromResult(ContentToExecution[contentId]);

    public Task<bool> CanInstructorEditContentAsync(Guid contentId, Guid instructorId, CancellationToken ct) =>
        Task.FromResult(Editors.Contains((contentId, instructorId)));

    public Task<bool> AllGroupsBelongToCourseExecutionAsync(List<Guid> groupIds, Guid executionId, CancellationToken ct) =>
        Task.FromResult(groupIds.Count > 0 &&
            groupIds.All(g => GroupToExecution.TryGetValue(g, out var e) && e == executionId));
}

public class AudienceApiFactory : WebApplicationFactory<Program>
{
    public FakeAudienceLookup Lookup { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IContentLookupForAudience>();
            services.RemoveAll<IGroupLookupForAudience>();
            services.AddSingleton<IContentLookupForAudience>(Lookup);
            services.AddSingleton<IGroupLookupForAudience>(Lookup);

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            services.Configure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = "Test";
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
                o.DefaultForbidScheme = "Test";
            });
        });
    }
}

[TestFixture]
public class ContentTargetAudienceEndpointTests
{
    private AudienceApiFactory _f = null!;

    [OneTimeSetUp]
    public void Init() => _f = new AudienceApiFactory();

    [OneTimeTearDown]
    public void Cleanup() => _f.Dispose();

    private HttpClient Client(Guid? userId = null)
    {
        var c = _f.CreateClient();
        if (userId != null) c.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
        return c;
    }

    private (Guid content, Guid instructor, Guid execution) Seed()
    {
        var content = Guid.NewGuid();
        var instructor = Guid.NewGuid();
        var exec = Guid.NewGuid();
        _f.Lookup.ContentToExecution[content] = exec;
        _f.Lookup.Editors.Add((content, instructor));
        return (content, instructor, exec);
    }

    [Test]
    public async Task Anonymous_Returns401()
    {
        var r = await Client().PostAsJsonAsync($"/contents/{Guid.NewGuid()}/target-audience",
            new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task InstructorWhoDoesNotOwnContent_Returns403()
    {
        var (content, _, _) = Seed();
        var r = await Client(Guid.NewGuid()).PostAsJsonAsync($"/contents/{content}/target-audience",
            new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UnknownContent_Returns404()
    {
        var r = await Client(Guid.NewGuid()).PostAsJsonAsync($"/contents/{Guid.NewGuid()}/target-audience",
            new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task WholeCourseExecution_Returns200()
    {
        var (content, instructor, _) = Seed();
        var r = await Client(instructor).PostAsJsonAsync($"/contents/{content}/target-audience",
            new { IsForWholeCourseExecution = true, TargetGroupIds = new List<Guid>() });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task SelectedGroupsOfSameExecution_Returns200()
    {
        var (content, instructor, exec) = Seed();
        var g = Guid.NewGuid();
        _f.Lookup.GroupToExecution[g] = exec;
        var r = await Client(instructor).PostAsJsonAsync($"/contents/{content}/target-audience",
            new { IsForWholeCourseExecution = false, TargetGroupIds = new List<Guid> { g } });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task EmptyGroupListWhenNotWholeExecution_Returns400()
    {
        var (content, instructor, _) = Seed();
        var r = await Client(instructor).PostAsJsonAsync($"/contents/{content}/target-audience",
            new { IsForWholeCourseExecution = false, TargetGroupIds = new List<Guid>() });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GroupFromAnotherExecution_Returns409()
    {
        var (content, instructor, _) = Seed();
        var foreign = Guid.NewGuid();
        _f.Lookup.GroupToExecution[foreign] = Guid.NewGuid();
        var r = await Client(instructor).PostAsJsonAsync($"/contents/{content}/target-audience",
            new { IsForWholeCourseExecution = false, TargetGroupIds = new List<Guid> { foreign } });
        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }
}