using Application.Staff;
using Domain.Academic;
using Domain.Staff;
using Infrastructure.Academic;   // PROVERI
using Infrastructure.Staff;
using NUnit.Framework;

namespace Tests.Staff;

[TestFixture]
public class CollaboratorTests
{
    private InMemoryCourseOfferingRepository _offerings = null!;   // PROVERI
    private InMemoryTeachingAssignmentRepository _assignments = null!;
    private InMemoryUserDirectory _users = null!;
    private TeachingStaffQueries _queries = null!;

    private UserInfo _petar = null!;     // odgovorni nastavnik
    private UserInfo _ana = null!;       // nastavnik sa drugog izvođenja
    private UserInfo _asistent = null!;
    private UserInfo _marko = null!;     // student

    [SetUp]
    public void SetUp()
    {
        _offerings = new InMemoryCourseOfferingRepository();   // PROVERI
        _assignments = new InMemoryTeachingAssignmentRepository();
        _users = new InMemoryUserDirectory();
        _queries = new TeachingStaffQueries(_assignments, _users);

        _petar = AddUser("petar", UserRole.Teacher);
        _ana = AddUser("ana", UserRole.Teacher);
        _asistent = AddUser("asistent", UserRole.Assistant);
        _marko = AddUser("marko", UserRole.Student);
    }

    private UserInfo AddUser(string name, UserRole role, bool active = true)
    {
        var user = new UserInfo(Guid.NewGuid(), name, name, "Test", role, active);
        _users.Add(user);
        return user;
    }

    private async Task<Guid> CreateOfferingAsync(OfferingState state = OfferingState.Active)
    {
        var offering = new CourseOffering(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        if (state is OfferingState.Active or OfferingState.Completed)
        {
            offering.ChangeState(OfferingState.Active);
        }

        if (state == OfferingState.Completed)
        {
            offering.ChangeState(OfferingState.Completed);
        }

        await _offerings.AddAsync(offering);
        return offering.Id;
    }

    // Odgovorni nastavnik se dodeljuje u delu ST-005 koji radi drugi član tima;
    // ovde se samo priprema stanje.
    private async Task SeedResponsibleAsync(Guid offering, UserInfo user)
    {
        await _assignments.AddAsync(new TeachingAssignment(
            Guid.NewGuid(), offering, user.Id, Responsibility.ResponsibleTeacher));
    }

    private AddCollaboratorHandler Add() => new(_offerings, _assignments, _users, _queries);
    private ListStaffHandler List() => new(_offerings, _assignments, _users, _queries);

    // ---------- pozitivni scenariji ----------

    [Test]
    public async Task Responsible_teacher_adds_assistant_and_list_shows_both()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);

        var added = await Add().HandleAsync(new(_petar.Id, offering, _asistent.Id, Responsibility.Assistant));
        var list = await List().HandleAsync(new(_petar.Id, offering));

        Assert.That(added.Responsibility, Is.EqualTo(Responsibility.Assistant));
        Assert.That(list.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Duplicate_add_does_not_create_second_row()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);

        await Add().HandleAsync(new(_petar.Id, offering, _asistent.Id, Responsibility.Assistant));
        await Add().HandleAsync(new(_petar.Id, offering, _asistent.Id, Responsibility.Assistant));

        var active = await _assignments.ListActiveByOfferingAsync(offering);
        Assert.That(active.Count(a => a.UserId == _asistent.Id), Is.EqualTo(1));
    }

    // ---------- negativni scenariji ----------

    [Test]
    public async Task Teacher_of_other_offering_cannot_add_collaborator()
    {
        var ers = await CreateOfferingAsync();
        var other = await CreateOfferingAsync();
        await SeedResponsibleAsync(ers, _petar);
        await SeedResponsibleAsync(other, _ana);

        Assert.ThrowsAsync<AccessDeniedException>(
            () => Add().HandleAsync(new(_ana.Id, ers, _asistent.Id, Responsibility.Assistant)));

        // odbijena operacija ne menja podatke
        var active = await _assignments.ListActiveByOfferingAsync(ers);
        Assert.That(active.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Assistant_cannot_add_collaborator()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);
        await Add().HandleAsync(new(_petar.Id, offering, _asistent.Id, Responsibility.Assistant));

        Assert.ThrowsAsync<AccessDeniedException>(
            () => Add().HandleAsync(new(_asistent.Id, offering, _ana.Id, Responsibility.Teacher)));
    }

    [Test]
    public async Task Student_cannot_be_added_as_collaborator()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => Add().HandleAsync(new(_petar.Id, offering, _marko.Id, Responsibility.Assistant)));
    }

    [Test]
    public async Task Deactivated_account_cannot_be_added()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);
        var inactive = AddUser("neaktivan", UserRole.Assistant, active: false);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => Add().HandleAsync(new(_petar.Id, offering, inactive.Id, Responsibility.Assistant)));
    }

    [Test]
    public async Task Responsible_role_cannot_be_given_through_collaborator_flow()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);

        Assert.ThrowsAsync<ArgumentException>(
            () => Add().HandleAsync(new(_petar.Id, offering, _ana.Id, Responsibility.ResponsibleTeacher)));
    }

    [Test]
    public async Task Completed_offering_rejects_adding()
    {
        var offering = await CreateOfferingAsync(OfferingState.Completed);
        await SeedResponsibleAsync(offering, _petar);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => Add().HandleAsync(new(_petar.Id, offering, _asistent.Id, Responsibility.Assistant)));
    }

    [Test]
    public void Unknown_offering_is_not_found()
    {
        Assert.ThrowsAsync<NotFoundException>(
            () => Add().HandleAsync(new(_petar.Id, Guid.NewGuid(), _asistent.Id, Responsibility.Assistant)));
    }

    [Test]
    public async Task Non_staff_cannot_list_staff()
    {
        var offering = await CreateOfferingAsync();
        await SeedResponsibleAsync(offering, _petar);

        Assert.ThrowsAsync<AccessDeniedException>(
            () => List().HandleAsync(new(_marko.Id, offering)));
    }
}
