using System.Net;
using System.Text.Json;
using Application;
using Application.Groups;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Demo;
using Infrastructure.Groups;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// HTTP testovi ST-019 dodavanja studenta u grupu nad stvarnom konfiguracijom
/// (bez zamene IGroupLookup / IStudentLookup test dvojnicima): provere postojanja grupe
/// i statusa studenta moraju stvarno da odbijaju, a demo adapteri ne smeju van
/// okruzenja Development i Testing.
/// </summary>
[TestFixture]
public class GroupLookupWiringTests
{
    private static string AddStudentUrl(string prefix, Guid groupId) => $"{prefix}/{groupId}/students";

    private static object AddBody(Guid studentId) => new { StudentId = studentId, StartDate = DateTime.UtcNow };

    private static async Task<List<Guid>> ListGroupIdsAsync(HttpClient client, Guid studentId)
    {
        var response = await client.SendAsync(
            ApiTestHost.AdminRequest(HttpMethod.Get, $"/api/Groups/students/{studentId}/groups"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray()
            .Select(group => group.GetProperty("id").GetGuid())
            .ToList();
    }

    private static Action<IServiceCollection> UseGroups(params Group[] groups)
    {
        return services =>
        {
            services.RemoveAll<IGroupRepository>();
            services.AddSingleton<IGroupRepository>(new InMemoryGroupRepository(groups));
        };
    }

    /* ---------------- Development / Testing: demo adapteri sa stvarnim odbijanjem ---------------- */

    [TestCase("Development", "/groups")]
    [TestCase("Development", "/api/Groups")]
    [TestCase("Testing", "/groups")]
    [TestCase("Testing", "/api/Groups")]
    public async Task AddStudent_ExistingGroupAndActiveStudent_ReturnsOk_AndCreatesMembership(
        string environment, string prefix)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl(prefix, DemoData.LecturesGroupId),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(
            await ListGroupIdsAsync(client, DemoData.ActiveStudentId),
            Is.EqualTo(new[] { DemoData.LecturesGroupId }));
    }

    [TestCase("Development", "/groups")]
    [TestCase("Development", "/api/Groups")]
    [TestCase("Testing", "/groups")]
    [TestCase("Testing", "/api/Groups")]
    public async Task AddStudent_NonexistentGroup_ReturnsNotFound_AndCreatesNothing(
        string environment, string prefix)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl(prefix, Guid.NewGuid()),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("grupa ne postoji"));
        Assert.That(await ListGroupIdsAsync(client, DemoData.ActiveStudentId), Is.Empty);
    }

    [TestCase("Development", "/groups")]
    [TestCase("Development", "/api/Groups")]
    [TestCase("Testing", "/groups")]
    [TestCase("Testing", "/api/Groups")]
    public async Task AddStudent_InactiveStudent_ReturnsConflict_AndCreatesNothing(
        string environment, string prefix)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl(prefix, DemoData.LecturesGroupId),
            AddBody(DemoData.InactiveStudentId)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("nije aktivan"));
        Assert.That(await ListGroupIdsAsync(client, DemoData.InactiveStudentId), Is.Empty);
    }

    [TestCase("/groups")]
    [TestCase("/api/Groups")]
    public async Task AddStudent_UnknownStudent_ReturnsConflict_AndCreatesNothing(string prefix)
    {
        var unknownStudentId = Guid.NewGuid();
        using var factory = ApiTestHost.Create("Testing");
        using var client = factory.CreateClient();

        var response = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl(prefix, DemoData.LecturesGroupId),
            AddBody(unknownStudentId)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ListGroupIdsAsync(client, unknownStudentId), Is.Empty);
    }

    [Test]
    public async Task AddStudent_LecturesAndExercisesGroupOfSameCourse_BothSucceed()
    {
        using var factory = ApiTestHost.Create("Testing");
        using var client = factory.CreateClient();

        var lectures = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl("/groups", DemoData.LecturesGroupId),
            AddBody(DemoData.ActiveStudentId)));
        var exercises = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl("/groups", DemoData.ExercisesGroupId),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(lectures.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(exercises.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(
            await ListGroupIdsAsync(client, DemoData.ActiveStudentId),
            Is.EquivalentTo(new[] { DemoData.LecturesGroupId, DemoData.ExercisesGroupId }));
    }

    // Pravila "ista vrsta" i "razlicito izvodjenje" moraju da rade preko HTTP-a sa stvarnim
    // IGroupLookup-om (ranije je GetByIdAsync uvek vracao null pa se nikad nisu primenjivala).

    [Test]
    public async Task AddStudent_SecondActiveMembershipOfSameType_ReturnsConflict()
    {
        var secondLectures = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Grupa 3",
            Type = GroupType.Predavanja,
            CourseId = DemoData.ErsCourseId
        };
        using var factory = ApiTestHost.Create(
            "Testing", UseGroups(DemoData.CreateGroups().Append(secondLectures).ToArray()));
        using var client = factory.CreateClient();

        var first = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl("/groups", DemoData.LecturesGroupId),
            AddBody(DemoData.ActiveStudentId)));
        var second = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl("/groups", secondLectures.Id),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await second.Content.ReadAsStringAsync(), Does.Contain("iste vrste"));
        Assert.That(
            await ListGroupIdsAsync(client, DemoData.ActiveStudentId),
            Is.EqualTo(new[] { DemoData.LecturesGroupId }));
    }

    [Test]
    public async Task AddStudent_GroupOfDifferentCourseExecution_ReturnsConflict()
    {
        var otherCourseExercises = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Grupa drugog predmeta",
            Type = GroupType.Vezbe,
            CourseId = Guid.NewGuid()
        };
        using var factory = ApiTestHost.Create(
            "Testing", UseGroups(DemoData.CreateGroups().Append(otherCourseExercises).ToArray()));
        using var client = factory.CreateClient();

        var first = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl("/groups", DemoData.LecturesGroupId),
            AddBody(DemoData.ActiveStudentId)));
        var second = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl("/groups", otherCourseExercises.Id),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await second.Content.ReadAsStringAsync(), Does.Contain("razlicitih izvodjenja"));
        Assert.That(
            await ListGroupIdsAsync(client, DemoData.ActiveStudentId),
            Is.EqualTo(new[] { DemoData.LecturesGroupId }));
    }

    /* ---------------- Ostala okruzenja: bez demo adaptera, odbijanje po defaultu ---------------- */

    [TestCase("Production", "/groups")]
    [TestCase("Production", "/api/Groups")]
    [TestCase("Staging", "/groups")]
    [TestCase("Staging", "/api/Groups")]
    public async Task OutsideDemoEnvironments_DemoGroupDoesNotExist_ReturnsNotFound(
        string environment, string prefix)
    {
        using var factory = ApiTestHost.Create(environment);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl(prefix, DemoData.LecturesGroupId),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ListGroupIdsAsync(client, DemoData.ActiveStudentId), Is.Empty);
    }

    [TestCase("Production", "/groups")]
    [TestCase("Production", "/api/Groups")]
    [TestCase("Staging", "/groups")]
    [TestCase("Staging", "/api/Groups")]
    public async Task OutsideDemoEnvironments_StudentStatusCannotBeConfirmed_ReturnsConflict(
        string environment, string prefix)
    {
        // Grupa postoji (stvarni izvor), ali nema stvarnog izvora o studentima:
        // provera mora da odbije, a ne da potvrdi svakog studenta.
        var realGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Stvarna grupa",
            Type = GroupType.Predavanja,
            CourseId = Guid.NewGuid()
        };
        using var factory = ApiTestHost.Create(environment, UseGroups(realGroup));
        using var client = factory.CreateClient();

        var response = await client.SendAsync(ApiTestHost.AdminRequest(
            HttpMethod.Post,
            AddStudentUrl(prefix, realGroup.Id),
            AddBody(DemoData.ActiveStudentId)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ListGroupIdsAsync(client, DemoData.ActiveStudentId), Is.Empty);
    }

    [TestCase("Development")]
    [TestCase("Testing")]
    public void DemoEnvironments_RegisterDemoStudentLookup_AndRepositoryBackedGroupLookup(string environment)
    {
        using var factory = ApiTestHost.Create(environment);

        Assert.That(factory.Services.GetRequiredService<IStudentLookup>(), Is.TypeOf<DemoStudentLookup>());
        Assert.That(factory.Services.GetRequiredService<IGroupLookup>(), Is.TypeOf<RepositoryGroupLookup>());
    }

    [TestCase("Production")]
    [TestCase("Staging")]
    public async Task OtherEnvironments_DoNotRegisterDemoAdapters(string environment)
    {
        using var factory = ApiTestHost.Create(environment);

        var studentLookup = factory.Services.GetRequiredService<IStudentLookup>();
        var groupLookup = factory.Services.GetRequiredService<IGroupLookup>();

        Assert.That(studentLookup, Is.TypeOf<UnavailableStudentLookup>());
        Assert.That(groupLookup, Is.TypeOf<RepositoryGroupLookup>());

        Assert.That(await studentLookup.IsActiveAsync(DemoData.ActiveStudentId, CancellationToken.None), Is.False);
        Assert.That(await studentLookup.IsActiveAsync(Guid.NewGuid(), CancellationToken.None), Is.False);
        Assert.That(await groupLookup.ExistsAsync(DemoData.LecturesGroupId, CancellationToken.None), Is.False);
        Assert.That(await groupLookup.ExistsAsync(Guid.NewGuid(), CancellationToken.None), Is.False);
    }
}
