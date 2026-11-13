using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.CourseEnrollments;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Tests.Api;

[TestFixture]
public sealed class EnrollmentsEndpointTests
{
    private static readonly Guid AuthorizedTeacherId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UnauthorizedTeacherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AllowedExecutionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StudentId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICourseExecutionPermissionChecker>();
                    services.AddSingleton<ICourseExecutionPermissionChecker>(
                        new StubPermissionChecker(AuthorizedTeacherId, AllowedExecutionId));

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestScheme";
                        options.DefaultChallengeScheme = "TestScheme";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
                });
            });
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task PostEnrollment_AnonymousUser_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enrollments", new
        {
            courseExecutionId = AllowedExecutionId,
            studentId = StudentId
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostEnrollment_SpoofedTeacherIdInBody_IgnoresBodyAndReturns403ForUnauthorizedPrincipal()
    {
        var client = _factory.CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme", UnauthorizedTeacherId.ToString());

        
        var response = await client.PostAsJsonAsync("/api/enrollments", new
        {
            teacherId = AuthorizedTeacherId,
            courseExecutionId = AllowedExecutionId,
            studentId = StudentId
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostEnrollment_AuthenticatedTeacherWithoutPermission_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme", UnauthorizedTeacherId.ToString());

        var response = await client.PostAsJsonAsync("/api/enrollments", new
        {
            courseExecutionId = AllowedExecutionId,
            studentId = StudentId
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostEnrollment_AuthorizedTeacherForExecution_Returns201Created()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme", AuthorizedTeacherId.ToString());

        var response = await client.PostAsJsonAsync("/api/enrollments", new
        {
            courseExecutionId = AllowedExecutionId,
            studentId = StudentId
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    private sealed class StubPermissionChecker(Guid allowedTeacher, Guid allowedExecution) : ICourseExecutionPermissionChecker
    {
        public Task<bool> CanTeacherManageExecutionAsync(Guid teacherId, Guid courseExecutionId, CancellationToken cancellationToken = default)
            => Task.FromResult(teacherId == allowedTeacher && courseExecutionId == allowedExecution);
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader)) { 
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var token = authHeader.ToString().Replace("TestScheme ", "", StringComparison.OrdinalIgnoreCase);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, token) };
            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
