using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Auth;
using Application.Users;
using Domain.Users;

[TestFixture]
public class ActivateAccountTests
{
    private class FakeUserLookup : IUserLookup
    {
        private readonly User? _user;
        public FakeUserLookup(User? user) => _user = user;
        public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) => Task.FromResult(user);
    }

    private class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed" + password;
        public bool Verify(string password, string hash) => hash == "hashed_" + password;
    }

    [Test]
    public async Task ActivateAccount_Success_SetsPasswordAndInvalidatesToken()
    {
        var user = new User(
            Guid.NewGuid(),
            "andrija",
            UserRole.Student,
            null,
            false,
            "token123",
            DateTime.UtcNow.AddHours(1)
        );

        var handler = new ActivateAccountHandler(new FakeUserLookup(user), new FakePasswordHasher());
        var command = new ActivateAccountCommand
        {
            Username = "andrija",
            Token = "token123",
            Password = "Sifra123"
        };

        bool result = await handler.HandleAsync(command);
dotnet run --project src/ConsoleUi
        Assert.IsTrue(result);
        Assert.IsTrue(user.IsActive);
        Assert.IsNull(user.ActivationToken);
        Assert.AreEqual("hashed_Sifra123", user.PasswordHash);
    }
}
