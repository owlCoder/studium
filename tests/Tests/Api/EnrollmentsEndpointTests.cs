using System.Net;
using System.Net.Http.Json;
using Api.Authentication;
using Application.DTOs;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests.Api;


[TestFixture]
public sealed class EnrollmentsEndpointTests
{
    private const string Url = "/api/enrollments";

    
    private static readonly Guid AlreadyEnrolledStudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TerminatedStudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private void LoginAs(Guid userId) => LoginAs(userId.ToString());

    private void LoginAs(string userId) =>
        _client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, userId);

    private Task<HttpResponseMessage> PostEnrollment(Guid courseExecutionId, Guid studentId) =>
        _client.PostAsJsonAsync(Url, new { courseExecutionId, studentId });

    // --- Negativni: autentifikacija ---

    [Test]
    public async Task Post_AnonymousUser_Returns401()
    {
        var response = await PostEnrollment(DemoData.ErsId, Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Post_InvalidUserId_Returns401()
    {
        LoginAs("nije-guid");

        var response = await PostEnrollment(DemoData.ErsId, Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Post_OutsideKt1Host_Returns401()
    {
        using var productionFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient();
        client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, DemoData.ErsTeacherId.ToString());

        var response = await client.PostAsJsonAsync(Url, new { courseExecutionId = DemoData.ErsId, studentId = Guid.NewGuid() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
            "Demo autentifikacija ne sme raditi van KT1 test hosta.");
    }

   

    [Test]
    public async Task Post_TeacherNotAssignedToOffering_Returns403()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await PostEnrollment(DemoData.OibId, Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Post_UnknownTeacher_Returns403()
    {
        LoginAs(Guid.NewGuid());

        var response = await PostEnrollment(DemoData.ErsId, Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Post_SpoofedTeacherIdInBody_IsIgnored_Returns403()
    {
        LoginAs(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync(Url, new
        {
            teacherId = DemoData.ErsTeacherId,
            courseExecutionId = DemoData.ErsId,
            studentId = Guid.NewGuid()
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Post_EmptyStudentId_Returns400()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await PostEnrollment(DemoData.ErsId, Guid.Empty);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    

    [Test]
    public async Task Post_AssignedTeacher_Returns201_AndStudentAppearsInList()
    {
        LoginAs(DemoData.ErsTeacherId);
        var studentId = Guid.NewGuid();

        var response = await PostEnrollment(DemoData.ErsId, studentId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var list = await _client.GetFromJsonAsync<List<StudentEnrollmentDto>>($"/api/offerings/{DemoData.ErsId}/enrollments");
        Assert.That(list!.Exists(e => e.StudentId == studentId), Is.True,
            "Upisan student mora biti vidljiv u pretrazi istog izvođenja.");
    }

    [Test]
    public async Task Post_StudentWithTerminatedMembership_CanBeEnrolledAgain()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await PostEnrollment(DemoData.ErsId, TerminatedStudentId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    // --- Ponovljeni i konkurentni upis ---

    [Test]
    public async Task Post_StudentAlreadyEnrolled_Returns409()
    {
        LoginAs(DemoData.ErsTeacherId);

        var response = await PostEnrollment(DemoData.ErsId, AlreadyEnrolledStudentId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Post_SameStudentTwice_SecondReturns409()
    {
        LoginAs(DemoData.ErsTeacherId);
        var studentId = Guid.NewGuid();

        var first = await PostEnrollment(DemoData.ErsId, studentId);
        var second = await PostEnrollment(DemoData.ErsId, studentId);

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        });
    }

    [Test]
    public async Task Post_ConcurrentRequestsForSameStudent_OnlyOneSucceeds()
    {
        LoginAs(DemoData.ErsTeacherId);
        var studentId = Guid.NewGuid();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => PostEnrollment(DemoData.ErsId, studentId)));

        Assert.Multiple(() =>
        {
            Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.Created), Is.EqualTo(1));
            Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.Conflict), Is.EqualTo(19));
        });
    }
}
