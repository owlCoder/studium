using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Authentication;
using Application.DTOs;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class EnrollmentsApiTests
{
    private static readonly Guid ActiveEnrollmentId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

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

    private static string EnrollmentsUrl(Guid offeringId) => $"/api/offerings/{offeringId}/enrollments";

    private static string TerminateUrl(Guid offeringId, Guid enrollmentId) =>
        $"{EnrollmentsUrl(offeringId)}/{enrollmentId}/terminate";

    private void LoginAs(string userId)
    {
        _client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, userId);
    }


    [Test]
    public async Task Host_Starts_WithRegisteredAuthentication()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }


    [Test]
    public async Task Get_ReturnsUnauthorized_WithoutUser()
    {
        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.ErsId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Get_ReturnsUnauthorized_WithInvalidUserId()
    {
        LoginAs("nije-guid");

        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.ErsId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Patch_ReturnsUnauthorized_WithoutUser()
    {
        var response = await _client.PatchAsync(TerminateUrl(DemoData.ErsId, ActiveEnrollmentId), null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Get_ReturnsUnauthorized_OutsideKt1TestHost()
    {
        using var productionFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient();
        client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, DemoData.ErsTeacherId.ToString());

        var response = await client.GetAsync(EnrollmentsUrl(DemoData.ErsId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
            "Demo autentifikacija ne sme raditi van KT1 test hosta.");
    }


    [Test]
    public async Task Get_ReturnsForbidden_ForTeacherNotAssignedToOffering()
    {
        LoginAs(DemoData.ErsTeacherId.ToString());

        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.OibId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Patch_ReturnsForbidden_ForTeacherNotAssignedToOffering()
    {
        LoginAs(Guid.NewGuid().ToString());

        var response = await _client.PatchAsync(TerminateUrl(DemoData.ErsId, ActiveEnrollmentId), null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }


    [Test]
    public async Task Get_ReturnsActiveEnrollments_ForAssignedTeacher()
    {
        LoginAs(DemoData.ErsTeacherId.ToString());

        var response = await _client.GetAsync(EnrollmentsUrl(DemoData.ErsId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<StudentEnrollmentDto>>();
        Assert.That(body, Has.Count.EqualTo(2), "Prekinuto članstvo ne sme biti na spisku.");
    }

    [Test]
    public async Task Patch_TerminatesEnrollment_AndRemovesItFromList()
    {
        LoginAs(DemoData.ErsTeacherId.ToString());

        var patch = await _client.PatchAsync(TerminateUrl(DemoData.ErsId, ActiveEnrollmentId), null);
        Assert.That(patch.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var list = await _client.GetFromJsonAsync<List<StudentEnrollmentDto>>(EnrollmentsUrl(DemoData.ErsId));
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list!.Exists(e => e.EnrollmentId == ActiveEnrollmentId), Is.False);
    }

    [Test]
    public async Task Patch_ReturnsNotFound_ForUnknownEnrollment()
    {
        LoginAs(DemoData.ErsTeacherId.ToString());

        var response = await _client.PatchAsync(TerminateUrl(DemoData.ErsId, Guid.NewGuid()), null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
