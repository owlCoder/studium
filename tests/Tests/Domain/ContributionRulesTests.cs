using NUnit.Framework;
using Domain.Models;

namespace Tests.Domain;

[TestFixture]
public sealed class ContributionRulesTests
{
    [Test]
    public void CanCreateContribution_ReturnsTrue_WhenTaskExists()
    {
        var existingTasks = new List<int> { 1, 2, 3 };

        var result = ContributionRules.CanCreateContribution(2, existingTasks, out var error);

        Assert.That(result, Is.True);
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void CanCreateContribution_ReturnsFalse_WhenTaskDoesNotExist()
    {
        var existingTasks = new List<int> { 1, 2, 3 };

        var result = ContributionRules.CanCreateContribution(99, existingTasks, out var error);

        Assert.That(result, Is.False);
        Assert.That(error, Does.Contain("ne postoji"));
    }

    [Test]
    public void CanCreateContribution_ReturnsFalse_WhenTaskIdIsInvalid()
    {
        var existingTasks = new List<int> { 1, 2, 3 };

        var result = ContributionRules.CanCreateContribution(0, existingTasks, out var error);

        Assert.That(result, Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    [TestCase(99, false)]
    [TestCase(0, false)]
    [TestCase(-5, false)]
    public void CanCreateContribution_ChecksTaskId(int taskId, bool expected)
    {
        var existingTasks = new List<int> { 1, 2, 3 };

        var result = ContributionRules.CanCreateContribution(taskId, existingTasks, out var error);

        TestContext.Out.WriteLine(error);
        Assert.That(result, Is.EqualTo(expected), error);
    }
}
