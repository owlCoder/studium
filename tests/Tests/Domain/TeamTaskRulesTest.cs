using NUnit.Framework;
using Domain.Models;

namespace Tests.Domain;

[TestFixture]
public sealed class TeamTaskRulesTests
{
    private readonly Guid _postojeciTim = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nepostojeciTim = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Test]
    public void CanCreateTask_ReturnsTrue_WhenTeamExists()
    {
        var timovi = new List<Guid> { _postojeciTim };

        var result = TeamTaskRules.CanCreateTask(_postojeciTim, timovi, out var error);

        TestContext.Out.WriteLine($"Rezultat: {result}, poruka: '{error}'");
        Assert.That(result, Is.True);
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void CanCreateTask_ReturnsFalse_WhenTeamDoesNotExist()
    {
        var timovi = new List<Guid> { _postojeciTim };

        var result = TeamTaskRules.CanCreateTask(_nepostojeciTim, timovi, out var error);

        TestContext.Out.WriteLine($"Rezultat: {result}, poruka: '{error}'");
        Assert.That(result, Is.False);
        Assert.That(error, Is.EqualTo($"Tim sa ID {_nepostojeciTim} ne postoji."));
    }

    [Test]
    public void CanCreateTask_ReturnsFalse_WhenTeamIdIsEmpty()
    {
        var timovi = new List<Guid> { _postojeciTim };

        var result = TeamTaskRules.CanCreateTask(Guid.Empty, timovi, out var error);

        TestContext.Out.WriteLine($"Rezultat: {result}, poruka: '{error}'");
        Assert.That(result, Is.False);
        Assert.That(error, Is.EqualTo("Zaduženje mora biti vezano za tim."));
    }
}
