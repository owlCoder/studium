using Application.Identity;
using Infrastructure.Demo;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class CurrentUserTests
{
    [Test]
    public void RequireRole_AllowsAdministrator()
    {
        var user = FakeCurrentUser.Administrator();

        Assert.That(() => user.RequireRole(UserRole.Administrator), Throws.Nothing);
    }

    [Test]
    public void RequireRole_RejectsStudentForAdministratorOperation()
    {
        var user = FakeCurrentUser.Student();

        Assert.That(
            () => user.RequireRole(UserRole.Administrator),
            Throws.TypeOf<UnauthorizedAccessException>());
    }
}
