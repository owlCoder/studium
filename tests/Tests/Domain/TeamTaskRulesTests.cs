using NUnit.Framework;
using Domain.Models;

namespace Tests.Domain;

[TestFixture]
public sealed class TeamTaskRulesTests
{
    [Test]
    public void CanCreateTask_ReturnsTrue_WhenTeamExists()
    {
        var teamId = Guid.NewGuid();
        var existingTeams = new List<Guid> { teamId };

        var result = TeamTaskRules.CanCreateTask(teamId, existingTeams, out var error);

        Assert.That(result, Is.True);
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void CanCreateTask_ReturnsFalse_WhenTeamDoesNotExist()
    {
        var existingTeams = new List<Guid> { Guid.NewGuid() };
        var unknownTeamId = Guid.NewGuid();

        var result = TeamTaskRules.CanCreateTask(unknownTeamId, existingTeams, out var error);

        Assert.That(result, Is.False);
        Assert.That(error, Does.Contain("ne postoji"));
    }

    [Test]
    public void CanCreateTask_ReturnsFalse_WhenTeamIdIsEmpty()
    {
        var existingTeams = new List<Guid> { Guid.NewGuid() };

        var result = TeamTaskRules.CanCreateTask(Guid.Empty, existingTeams, out var error);

        Assert.That(result, Is.False);
        Assert.That(error, Is.Not.Empty);
    }
}
