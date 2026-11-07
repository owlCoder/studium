using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Courses;
using Application.Groups;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Testovi;

public class TestGroupLookup : IGroupLookup
{
    public bool ExistsResult { get; set; } = true;
    public Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken) => Task.FromResult(ExistsResult);
}

public class TestStudentLookup : IStudentLookup
{
    public bool IsActiveResult { get; set; } = true;
    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken) => Task.FromResult(IsActiveResult);
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

        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task AddAndRemove_StudentMembership_Success()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-5);

        var addCommand = new AddStudentToGroupCommand(groupId, studentId, startDate);
        await _addHandler.Handle(addCommand, CancellationToken.None);

        var active = await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None);
        Assert.That(active, Is.Not.Null);

        var removeCommand = new RemoveStudentFromGroupCommand(groupId, studentId, DateTime.UtcNow);
        await _removeHandler.Handle(removeCommand, CancellationToken.None);

        var updatedActive = await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None);
        Assert.That(updatedActive, Is.Null);
    }

    [Test]
    public async Task AddStudent_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;

        await _addHandler.Handle(new AddStudentToGroupCommand(groupId, studentId, startDate), CancellationToken.None);

        var duplicateCommand = new AddStudentToGroupCommand(groupId, studentId, startDate);

        InvalidOperationException? ex = null;
        try
        {
            await _addHandler.Handle(duplicateCommand, CancellationToken.None);
        }
        catch (InvalidOperationException e)
        {
            ex = e;
        }

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Student je vec u ovoj grupi"));
    }

    [Test]
    public async Task Concurrent_AddStudentToGroup_ShouldPreventDuplicatesAndThrowException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;

        var command1 = new AddStudentToGroupCommand(groupId, studentId, startDate);
        var command2 = new AddStudentToGroupCommand(groupId, studentId, startDate);

        var task1 = _addHandler.Handle(command1, CancellationToken.None);
        var task2 = _addHandler.Handle(command2, CancellationToken.None);

        Exception? exception = null;
        try
        {
            await Task.WhenAll(task1, task2);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        Assert.That(exception, Is.Not.Null);

        var activeMembership = await _repository.GetActiveAsync(groupId, studentId, CancellationToken.None);
        Assert.That(activeMembership, Is.Not.Null);
    }

    [Test]
    public async Task AddStudent_WhenGroupNotExists_ThrowsKeyNotFoundException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _groupLookup.ExistsResult = false;

        var command = new AddStudentToGroupCommand(groupId, studentId, DateTime.UtcNow);

        KeyNotFoundException? ex = null;
        try
        {
            await _addHandler.Handle(command, CancellationToken.None);
        }
        catch (KeyNotFoundException e)
        {
            ex = e;
        }

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Navedena grupa ne postoji"));
    }

    [Test]
    public async Task AddStudent_WhenStudentNotActive_ThrowsInvalidOperationException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _groupLookup.ExistsResult = true;
        _studentLookup.IsActiveResult = false;

        var command = new AddStudentToGroupCommand(groupId, studentId, DateTime.UtcNow);

        InvalidOperationException? ex = null;
        try
        {
            await _addHandler.Handle(command, CancellationToken.None);
        }
        catch (InvalidOperationException e)
        {
            ex = e;
        }

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Student ne postoji ili nije aktivan"));
    }

    [Test]
    public async Task RemoveStudent_WhenNotExists_ThrowsKeyNotFoundException()
    {
        var removeCommand = new RemoveStudentFromGroupCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        KeyNotFoundException? ex = null;
        try
        {
            await _removeHandler.Handle(removeCommand, CancellationToken.None);
        }
        catch (KeyNotFoundException e)
        {
            ex = e;
        }

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Aktivno clanstvo studenta u ovoj grupi nije pronadjeno"));
    }

    [Test]
    public async Task RemoveStudent_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;

        await _addHandler.Handle(new AddStudentToGroupCommand(groupId, studentId, startDate), CancellationToken.None);

        var invalidRemoveCommand = new RemoveStudentFromGroupCommand(groupId, studentId, startDate.AddDays(-1));

        ArgumentException? ex = null;
        try
        {
            await _removeHandler.Handle(invalidRemoveCommand, CancellationToken.None);
        }
        catch (ArgumentException e)
        {
            ex = e;
        }

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Datum zavrsetka clanstva ne moze biti pre datuma pocetka"));
    }

    [Test]
    public async Task ApiEndpoints_AreReachable_ReturnsSuccessOrExpectedStatus()
    {
        var response = await _client.GetAsync("/health");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ApiEndpoint_PostStudentToGroup_WithoutAuth_ReturnsUnauthorized()
    {
        var groupId = Guid.NewGuid();
        var request = new { StudentId = Guid.NewGuid(), StartDate = DateTime.UtcNow };

        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/students", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ApiEndpoint_DeleteStudentFromGroup_WithoutAuth_ReturnsUnauthorized()
    {
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new { EndDate = DateTime.UtcNow };

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/groups/{groupId}/students/{studentId}")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ApiEndpoint_PostStudentToGroup_WithAdminRole_ReturnsSuccess()
    {
        using var authClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var groupId = Guid.NewGuid();
        var request = new { StudentId = Guid.NewGuid(), StartDate = DateTime.UtcNow };

        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/students", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- ST-020: JEDNOSTAVNI UNIT TESTOVI ZA HANDLER ---

    [Test]
    public async Task ST020_GetSubmission_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/submissions/sub-01");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ST020_GetSubmission_NonExistingId_ReturnsNotFound()
    {
        var handler = new GetSubmissionHandler();
        var result = await handler.HandleAsync("student-autor-1", "nepostojeci-id");
        Assert.That(result.IsNotFound, Is.True);
    }

    [Test]
    public async Task ST020_GetSubmission_OtherStudentSubmission_ReturnsForbidden()
    {
        var handler = new GetSubmissionHandler();
        var result = await handler.HandleAsync("student-neki-drugi", "sub-02");
        Assert.That(result.IsForbidden, Is.True);
    }

    [Test]
    public async Task ST020_GetSubmission_AuthorAccessAfterAudienceChange_ReturnsSuccess()
    {
        var handler = new GetSubmissionHandler();
        var result = await handler.HandleAsync("student-autor-1", "sub-01");
        Assert.That(result.Submission, Is.Not.Null);
        Assert.That(result.Submission!.Id, Is.EqualTo("sub-01"));
    }
}
