using Domain.Entities;
using Infrastructure;
using Infrastructure.Demo;
using Infrastructure.Groups;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class GroupLookupAdaptersTests
{
    private static RepositoryGroupLookup CreateGroupLookup(params Group[] groups)
    {
        return new RepositoryGroupLookup(new InMemoryGroupRepository(groups));
    }

    [Test]
    public async Task RepositoryGroupLookup_ExistsAsync_TrueOnlyForGroupsFromRepository()
    {
        var group = new Group { Id = Guid.NewGuid(), Name = "Grupa", Type = GroupType.Vezbe, CourseId = Guid.NewGuid() };
        var lookup = CreateGroupLookup(group);

        Assert.That(await lookup.ExistsAsync(group.Id, CancellationToken.None), Is.True);
        Assert.That(await lookup.ExistsAsync(Guid.NewGuid(), CancellationToken.None), Is.False);
        Assert.That(await lookup.ExistsAsync(Guid.Empty, CancellationToken.None), Is.False);
    }

    [Test]
    public async Task RepositoryGroupLookup_GetByIdAsync_ReturnsGroupOrNull()
    {
        var group = new Group { Id = Guid.NewGuid(), Name = "Grupa", Type = GroupType.Predavanja, CourseId = Guid.NewGuid() };
        var lookup = CreateGroupLookup(group);

        Assert.That(await lookup.GetByIdAsync(group.Id, CancellationToken.None), Is.SameAs(group));
        Assert.That(await lookup.GetByIdAsync(Guid.NewGuid(), CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task RepositoryGroupLookup_WithEmptyRepository_NeverFindsAnyGroup()
    {
        var lookup = CreateGroupLookup();

        Assert.That(await lookup.ExistsAsync(DemoData.LecturesGroupId, CancellationToken.None), Is.False);
        Assert.That(await lookup.GetByIdAsync(DemoData.LecturesGroupId, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task DemoStudentLookup_ConfirmsOnlyKnownActiveStudents()
    {
        var lookup = new DemoStudentLookup();

        Assert.That(await lookup.IsActiveAsync(DemoData.ActiveStudentId, CancellationToken.None), Is.True);
        Assert.That(await lookup.IsActiveAsync(DemoData.InactiveStudentId, CancellationToken.None), Is.False);
        Assert.That(await lookup.IsActiveAsync(Guid.NewGuid(), CancellationToken.None), Is.False);
        Assert.That(await lookup.IsActiveAsync(Guid.Empty, CancellationToken.None), Is.False);
    }

    [Test]
    public async Task DemoStudentLookup_UsesSuppliedRoster()
    {
        var active = Guid.NewGuid();
        var inactive = Guid.NewGuid();
        var lookup = new DemoStudentLookup(new Dictionary<Guid, bool>
        {
            [active] = true,
            [inactive] = false
        });

        Assert.That(await lookup.IsActiveAsync(active, CancellationToken.None), Is.True);
        Assert.That(await lookup.IsActiveAsync(inactive, CancellationToken.None), Is.False);
        Assert.That(await lookup.IsActiveAsync(DemoData.ActiveStudentId, CancellationToken.None), Is.False);
    }

    [Test]
    public async Task UnavailableStudentLookup_NeverConfirmsStudent()
    {
        var lookup = new UnavailableStudentLookup();

        Assert.That(await lookup.IsActiveAsync(DemoData.ActiveStudentId, CancellationToken.None), Is.False);
        Assert.That(await lookup.IsActiveAsync(Guid.NewGuid(), CancellationToken.None), Is.False);
    }
}
