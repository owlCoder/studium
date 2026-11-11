using NUnit.Framework;
using Infrastructure.Materials;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryOfferingStaffCheckerTests
{
    [Test]
    public async Task IsStaffOnOfferingAsync_ReturnsTrueForAssignedUserOnThatOffering()
    {
        var user = Guid.NewGuid();
        var offering = Guid.NewGuid();
        var checker = new InMemoryOfferingStaffChecker([(user, offering)]);

        Assert.That(await checker.IsStaffOnOfferingAsync(user, offering), Is.True);
    }

    [Test]
    public async Task IsStaffOnOfferingAsync_ReturnsFalseForOtherOffering()
    {
        var user = Guid.NewGuid();
        var checker = new InMemoryOfferingStaffChecker([(user, Guid.NewGuid())]);

        Assert.That(await checker.IsStaffOnOfferingAsync(user, Guid.NewGuid()), Is.False);
    }

    [Test]
    public async Task IsStaffOnOfferingAsync_DeniesEveryoneWhenNoAssignments()
    {
        var checker = new InMemoryOfferingStaffChecker();

        Assert.That(await checker.IsStaffOnOfferingAsync(Guid.NewGuid(), Guid.NewGuid()), Is.False);
    }
}
