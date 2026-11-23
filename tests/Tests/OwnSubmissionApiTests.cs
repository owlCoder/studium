using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Authentication;
using Application.Submissions;
using ConsoleUi.Scenarios;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public sealed class OwnSubmissionApiTests
{
    private static string Url => $"/api/assignments/{SubmissionDemoData.AssignmentId}/submissions/me";

    [Test]
    public async Task Get_ReturnsOnlyOwnSubmissionDetails()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        LoginAs(client, SubmissionDemoData.StudentId);
        var submission = SubmissionDemoData.CreateSubmission();
        await factory.Services.GetRequiredService<ISubmissionRepository>().AddAsync(submission);

        using var response = await client.GetAsync(Url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var attachment = body.GetProperty("attachments").EnumerateArray().Single();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("text").GetString(), Is.EqualTo(submission.Text));
            Assert.That(body.GetProperty("submittedAt").GetDateTimeOffset().EqualsExact(submission.SubmittedAt), Is.True);
            Assert.That(attachment.GetProperty("fileName").GetString(), Is.EqualTo("primer.pdf"));
            Assert.That(attachment.GetProperty("fileType").GetString(), Is.EqualTo("application/pdf"));
            Assert.That(attachment.GetProperty("size").GetInt64(), Is.EqualTo(2048));
            Assert.That(body.TryGetProperty("studentId", out _), Is.False);
        });
    }

    [Test]
    public async Task Get_ReturnsNotFoundWhenOwnSubmissionDoesNotExist()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        LoginAs(client, SubmissionDemoData.StudentId);

        using var response = await client.GetAsync(Url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("message").GetString(), Is.EqualTo("Nemaš predaju za ovaj zadatak."));
    }

    [Test]
    public async Task Get_QueryCannotSelectAnotherStudentsSubmission()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        LoginAs(client, Guid.NewGuid());
        await factory.Services.GetRequiredService<ISubmissionRepository>().AddAsync(SubmissionDemoData.CreateSubmission());

        using var response = await client.GetAsync($"{Url}?studentId={SubmissionDemoData.StudentId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Get_RequiresAuthentication()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(Url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("nije-guid")]
    [TestCase("00000000-0000-0000-0000-000000000000")]
    public async Task Get_RejectsInvalidAssignmentId(string assignmentId)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        LoginAs(client, SubmissionDemoData.StudentId);

        using var response = await client.GetAsync($"/api/assignments/{assignmentId}/submissions/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Production_DisablesDemoAuthenticationAndDemoSeed()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production").UseSetting("DemoSubmissions", "true"));
        using var client = factory.CreateClient();
        LoginAs(client, SubmissionDemoData.StudentId);

        using var response = await client.GetAsync(Url);
        var result = await factory.Services.GetRequiredService<ISubmissionRepository>()
            .FindCurrentAsync(SubmissionDemoData.AssignmentId, SubmissionDemoData.StudentId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(result, Is.Null);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Console_PrintsOwnSubmissionOrNotFound(bool seedDemoSubmission)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development").UseSetting("DemoSubmissions", seedDemoSubmission.ToString()));
        using var client = factory.CreateClient();
        using var input = new StringReader(SubmissionDemoData.AssignmentId.ToString());
        using var output = new StringWriter();

        await ViewOwnSubmissionScenario.RunAsync(client, input, output);

        var display = output.ToString();
        if (seedDemoSubmission)
        {
            Assert.That(display, Does.Contain("Primer predaje individualnog rada.")
                .And.Contain("primer.pdf (application/pdf, 2048 B)")
                .And.Contain("23.11.2026 12:00:00 +01:00"));
        }
        else
        {
            Assert.That(display, Does.Contain("Nemaš predaju za ovaj zadatak."));
        }
    }

    private static void LoginAs(HttpClient client, Guid studentId)
    {
        client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, studentId.ToString());
    }
}
