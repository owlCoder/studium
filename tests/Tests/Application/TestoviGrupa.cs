using System.Net;
using System.Net.Http.Json;
using Application.Groups;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Tests.Application;
using System.Security.Claims;

namespace Testovi;
 
public class TestGroupLookup : IGroupLookup
{
    public bool ExistsResult { get; set; } = true;
 
    public Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistsResult);
 
    public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken) =>
        Task.FromResult<Group?>(null);
}
 
public class TestStudentLookup : IStudentLookup
{
    public bool IsActiveResult { get; set; } = true;
 
    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken) =>
        Task.FromResult(IsActiveResult);
}
 
[TestFixture]
public class TestoviGrupa
{
    private InMemoryGroupMembershipRepository _repository = null!;
    private TestGroupLookup _groupLookup = null!;
    private TestStudentLookup _studentLookup = null!;
    private AddStudentToGroupHandler _addHandler = null!;
    private RemoveStudentFromGroupHandler _removeHandler = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
 
    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryGroupMembershipRepository();
        _groupLookup = new TestGroupLookup();
        _studentLookup = new TestStudentLookup();
 
        _addHandler = new AddStudentToGroupHandler(_repository, _groupLookup, _studentLookup);
        _removeHandler = new RemoveStudentFromGroupHandler(_repository);
 
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IGroupMembershipRepository>();
                    services.RemoveAll<IGroupLookup>();
                    services.RemoveAll<IStudentLookup>();
 
                    services.AddSingleton<IGroupMembershipRepository>(_repository);
                    services.AddSingleton<IGroupLookup>(_groupLookup);
                    services.AddSingleton<IStudentLookup>(_studentLookup);
 
                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = "Test";
                            options.DefaultChallengeScheme = "Test";
                            options.DefaultForbidScheme = "Test";
                        })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
                });
            });
 
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
 
    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
 
    private static HttpRequestMessage Req(
        HttpMethod method,
        string url,
        object? body = null,
        Guid? user = null,
        string? role = null,
        Guid? allowedGroup = null)
    {
        var request = new HttpRequestMessage(method, url);
 
        if (user != null) request.Headers.Add("X-Test-User", user.Value.ToString());
        if (role != null) request.Headers.Add("X-Test-Role", role);
        if (allowedGroup != null) request.Headers.Add("X-Test-AllowedGroup", allowedGroup.Value.ToString());
        if (body != null) request.Content = JsonContent.Create(body);
 
        return request;
    }
 
    private static object AddBody() => new { StudentId = Guid.NewGuid(), StartDate = DateTime.UtcNow };
 
    private static object EndBody() => new { EndDate = DateTime.UtcNow };
 
    private async Task SeedActiveMembership(Guid groupId, Guid studentId)
    {
        await _repository.AddAsync(new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            StudentId = studentId,
            StartDate = DateTime.UtcNow.AddDays(-5)
        }, CancellationToken.None);
    }
 
    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
 
    [Test]
    public async Task AddAndRemove_StudentMembership_Success()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
 
        await _addHandler.Handle(
            new AddStudentToGroupCommand(groupId, studentId, DateTime.UtcNow.AddDays(-5)),
            CancellationToken.None);
 
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Not.Null);
 
        await _removeHandler.Handle(
            new RemoveStudentFromGroupCommand(groupId, studentId, DateTime.UtcNow),
            CancellationToken.None);
 
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Null);
    }
 
    [Test]
    public async Task AddStudent_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var command = new AddStudentToGroupCommand(groupId, studentId, DateTime.UtcNow);
 
        await _addHandler.Handle(command, CancellationToken.None);
 
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _addHandler.Handle(command, CancellationToken.None));
 
        Assert.That(ex!.Message, Does.Contain("Student je vec u ovoj grupi"));
    }
 
    [Test]
    public async Task Concurrent_AddStudentToGroup_ExactlyOneSucceeds()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var command = new AddStudentToGroupCommand(groupId, studentId, DateTime.UtcNow);
 
        var results = await Task.WhenAll(
            Capture(() => _addHandler.Handle(command, CancellationToken.None)),
            Capture(() => _addHandler.Handle(command, CancellationToken.None)));
 
        var failures = results.Where(e => e != null).ToList();
 
        Assert.That(failures, Has.Count.EqualTo(1), "Tacno jedan poziv mora da propadne.");
        Assert.That(failures[0], Is.TypeOf<InvalidOperationException>());
 
        var active = await _repository.GetActiveByStudentIdAsync(studentId, CancellationToken.None);
        Assert.That(active, Has.Count.EqualTo(1), "Student mora imati tacno jedno aktivno clanstvo.");
    }
 
    [Test]
    public void AddStudent_WhenGroupNotExists_ThrowsKeyNotFoundException()
    {
        _groupLookup.ExistsResult = false;
 
        var ex = Assert.ThrowsAsync<KeyNotFoundException>(() => _addHandler.Handle(
            new AddStudentToGroupCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow),
            CancellationToken.None));
 
        Assert.That(ex!.Message, Does.Contain("Navedena grupa ne postoji"));
    }
 
    [Test]
    public void AddStudent_WhenStudentNotActive_ThrowsInvalidOperationException()
    {
        _studentLookup.IsActiveResult = false;
 
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _addHandler.Handle(
            new AddStudentToGroupCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow),
            CancellationToken.None));
 
        Assert.That(ex!.Message, Does.Contain("Student ne postoji ili nije aktivan"));
    }
 
    [Test]
    public void RemoveStudent_WhenNotExists_ThrowsKeyNotFoundException()
    {
        var ex = Assert.ThrowsAsync<KeyNotFoundException>(() => _removeHandler.Handle(
            new RemoveStudentFromGroupCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow),
            CancellationToken.None));
 
        Assert.That(ex!.Message, Does.Contain("Aktivno clanstvo studenta u ovoj grupi nije pronadjeno"));
    }
 
    [Test]
    public async Task RemoveStudent_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
 
        await _addHandler.Handle(new AddStudentToGroupCommand(groupId, studentId, startDate), CancellationToken.None);
 
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _removeHandler.Handle(
            new RemoveStudentFromGroupCommand(groupId, studentId, startDate.AddDays(-1)),
            CancellationToken.None));
 
        Assert.That(ex!.Message, Does.Contain("Datum zavrsetka clanstva ne moze biti pre datuma pocetka"));
    }
 
    [Test]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
 
    [Test]
    public async Task Controller_PostStudentToGroup_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(
            Req(HttpMethod.Post, $"/api/Groups/{Guid.NewGuid()}/students", AddBody()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
 
    [Test]
    public async Task Controller_PostStudentToGroup_WithoutGroupPermission_ReturnsForbidden()
    {
        var response = await _client.SendAsync(
            Req(HttpMethod.Post, $"/api/Groups/{Guid.NewGuid()}/students", AddBody(), user: Guid.NewGuid()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
 
    [Test]
    public async Task Controller_DeleteStudentFromGroup_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(
            Req(HttpMethod.Delete, $"/api/Groups/{Guid.NewGuid()}/students/{Guid.NewGuid()}", EndBody()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
 
    [Test]
    public async Task Controller_GetStudentGroups_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(
            Req(HttpMethod.Get, $"/api/Groups/students/{Guid.NewGuid()}/groups"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
 
    [Test]
    public async Task Controller_GetStudentGroups_AnotherStudent_ReturnsForbidden()
    {
        var response = await _client.SendAsync(Req(
            HttpMethod.Get, $"/api/Groups/students/{Guid.NewGuid()}/groups", user: Guid.NewGuid()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
 
    [Test]
    public async Task Controller_GetStudentGroups_OwnGroups_ReturnsOk()
    {
        var studentId = Guid.NewGuid();
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Get, $"/api/Groups/students/{studentId}/groups", user: studentId));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
 
    [TestCase("/api/Groups")]
    [TestCase("/groups")]
    public async Task PostStudentToGroup_AssignedGroup_ReturnsOk_AndCreatesMembership(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{groupId}/students",
            new { StudentId = studentId, StartDate = DateTime.UtcNow },
            user: Guid.NewGuid(), allowedGroup: groupId));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Not.Null);
    }
 
    [TestCase("/api/Groups")]
    [TestCase("/groups")]
    public async Task PostStudentToGroup_DifferentGroup_ReturnsForbidden_AndCreatesNothing(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{groupId}/students",
            new { StudentId = studentId, StartDate = DateTime.UtcNow },
            user: Guid.NewGuid(), allowedGroup: Guid.NewGuid()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Null);
    }
 
    [TestCase("/api/Groups")]
    [TestCase("/groups")]
    public async Task PostStudentToGroup_Admin_ReturnsOk(string prefix)
    {
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{Guid.NewGuid()}/students", AddBody(),
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
 
    [Test]
    public async Task MinimalApi_PostStudentToGroup_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(
            Req(HttpMethod.Post, $"/groups/{Guid.NewGuid()}/students", AddBody()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
 
    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task PostStudentToGroup_UnknownGroup_ReturnsNotFound(string prefix)
    {
        _groupLookup.ExistsResult = false;
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{Guid.NewGuid()}/students", AddBody(),
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
 
    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task PostStudentToGroup_InactiveStudent_ReturnsConflict(string prefix)
    {
        _studentLookup.IsActiveResult = false;
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{Guid.NewGuid()}/students", AddBody(),
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }
 
    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task PostStudentToGroup_AlreadyMember_ReturnsConflict(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await SeedActiveMembership(groupId, studentId);
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{groupId}/students",
            new { StudentId = studentId, StartDate = DateTime.UtcNow },
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }
 
    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task PostStudentToGroup_EmptyStudentId_ReturnsBadRequest(string prefix)
    {
        var response = await _client.SendAsync(Req(
            HttpMethod.Post, $"{prefix}/{Guid.NewGuid()}/students",
            new { StudentId = Guid.Empty, StartDate = DateTime.UtcNow },
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("/api/Groups")]
    [TestCase("/groups")]
    public async Task DeleteStudentFromGroup_AssignedGroup_ReturnsOk_AndEndsMembership(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await SeedActiveMembership(groupId, studentId);
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Delete, $"{prefix}/{groupId}/students/{studentId}", EndBody(),
            user: Guid.NewGuid(), allowedGroup: groupId));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Null);
    }
 
    [TestCase("/api/Groups")]
    [TestCase("/groups")]
    public async Task DeleteStudentFromGroup_DifferentGroup_ReturnsForbidden_AndKeepsMembership(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await SeedActiveMembership(groupId, studentId);
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Delete, $"{prefix}/{groupId}/students/{studentId}", EndBody(),
            user: Guid.NewGuid(), allowedGroup: Guid.NewGuid()));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Not.Null);
    }
 
    [TestCase("/api/Groups")]
    [TestCase("/groups")]
    public async Task DeleteStudentFromGroup_Admin_ReturnsOk(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await SeedActiveMembership(groupId, studentId);
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Delete, $"{prefix}/{groupId}/students/{studentId}", EndBody(),
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
 
    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task DeleteStudentFromGroup_NoActiveMembership_ReturnsNotFound(string prefix)
    {
        var response = await _client.SendAsync(Req(
            HttpMethod.Delete, $"{prefix}/{Guid.NewGuid()}/students/{Guid.NewGuid()}", EndBody(),
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
 
    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task DeleteStudentFromGroup_EndDateBeforeStart_ReturnsBadRequest_AndKeepsMembership(string prefix)
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await SeedActiveMembership(groupId, studentId); 
 
        var response = await _client.SendAsync(Req(
            HttpMethod.Delete, $"{prefix}/{groupId}/students/{studentId}",
            new { EndDate = DateTime.UtcNow.AddDays(-10) },
            user: Guid.NewGuid(), role: "Admin"));
 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None), Is.Not.Null);
    }
[Test]
    public async Task DeleteStudent_AnonymousUser_ReturnsUnauthorized()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/groups/{groupId}/students/{studentId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DeleteStudent_InvalidAuthorizationHeader_ReturnsUnauthorized()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/groups/{groupId}/students/{studentId}");
        request.Headers.Add("Authorization", "Bearer invalid-or-fake-token");

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DeleteStudent_AuthenticatedUserWithoutRights_ReturnsForbidden()
    {
        var targetGroupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var clientWithoutRights = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "NoRightsScheme";
                    options.DefaultChallengeScheme = "NoRightsScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestNoRightsAuthHandler>("NoRightsScheme", options => { });
            });
        }).CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/groups/{targetGroupId}/students/{studentId}")
        {
            Content = JsonContent.Create(new { EndDate = DateTime.UtcNow })
        };

        var response = await clientWithoutRights.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

public class TestNoRightsAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestNoRightsAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "KorisnikBezPrava"),
            new Claim("AllowedGroup", Guid.NewGuid().ToString()) // Pravo na drugu grupu, nikako na ciljnu
        };
        var identity = new ClaimsIdentity(claims, "NoRightsScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "NoRightsScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

   protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}
}

