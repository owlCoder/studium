using Moq;
using NUnit.Framework;
using Application.Users;
using Domain.Users;
//using Infrastructure.Users;

namespace Tests.Application;

[TestFixture]
public sealed class CreateUserHandlerTests
{
    [Test]
    public async Task HandleAsync_CreatesUser_WhenDataIsValid()
    {
        // Arrange
        var repoMock = new Mock<IUserRepository>();
        // Moq po defaultu vraća null za GetByUsernameAsync, što znači "nema duplikata"

        var handler = new CreateUserHandler(repoMock.Object);
        var command = new CreateUserCommand("Ana", "Petrović", "Ana.Petrovic", UserRole.Student);

        // Act
        var user = await handler.HandleAsync(command);

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(user.FirstName, Is.EqualTo("Ana"));
        Assert.That(user.LastName, Is.EqualTo("Petrović"));
        Assert.That(user.Username, Is.EqualTo("ana.petrovic")); // Provera normalizacije
        Assert.That(user.Role, Is.EqualTo(UserRole.Student));

        // Provera da je AddAsync pozvan tačno jednom
        repoMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_NormalizesUsername_BeforeCheckingRepository()
    {
        // Arrange
        var repoMock = new Mock<IUserRepository>();
        var handler = new CreateUserHandler(repoMock.Object);
        var command = new CreateUserCommand("Ana", "Petrović", "  Ana.Petrovic  ", UserRole.Student);

        // Act
        await handler.HandleAsync(command);

        // Assert
        // Proveravamo da li je repozitorijum dobio već normalizovan username
        repoMock.Verify(x => x.GetByUsernameAsync("ana.petrovic", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase("", "Petrović", "ana")]
    [TestCase("Ana", "", "ana")]
    [TestCase("Ana", "Petrović", "")]
    [TestCase("  ", "Petrović", "ana")]
    public async Task HandleAsync_ThrowsArgumentException_WhenFieldsAreEmpty(
        string firstName, string lastName, string username)
    {
        // Arrange
        var repoMock = new Mock<IUserRepository>();
        var handler = new CreateUserHandler(repoMock.Object);
        var command = new CreateUserCommand(firstName, lastName, username, UserRole.Student);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await handler.HandleAsync(command));
    }

    [Test]
    public async Task HandleAsync_ThrowsInvalidOperationException_WhenUsernameIsDuplicate()
    {
        // Arrange
        var repoMock = new Mock<IUserRepository>();
        var existingUser = new User(Guid.NewGuid(), "Postojeći", "Korisnik", "ana.petrovic", UserRole.Student);

        // Simuliramo da repozitorijum već ima korisnika sa tim imenom
        repoMock.Setup(x => x.GetByUsernameAsync("ana.petrovic", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

        var handler = new CreateUserHandler(repoMock.Object);
        var command = new CreateUserCommand("Ana", "Petrović", "Ana.Petrovic", UserRole.Student);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(command));
        Assert.That(ex.Message, Does.Contain("duplikat").IgnoreCase);

        // Proveravamo da AddAsync NIJE pozvan jer je bacio grešku pre toga
        repoMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Ignore("Čeka se integracija PR #33 i #24. Test trenutno ne može da se izvrši jer InMemoryUserRepository nije atomaran.")]
    public async Task HandleAsync_OnlyOneUserCreated_WhenParallelRequestsHaveSameUsername()
    {/*
        // Arrange
        // Ovde koristimo PRAVI InMemoryUserRepository jer Moq ne može da testira thread-safety
        var repo = new InMemoryUserRepository();
        var handler = new CreateUserHandler(repo);
        const int parallelCount = 10;
        const string username = "test.user";

        // Barrier osigurava da svi taskovi krenu u isto vreme
        using var barrier = new Barrier(parallelCount);

        var tasks = Enumerable.Range(0, parallelCount).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            try
            {
                await handler.HandleAsync(new CreateUserCommand("Test", "User", username, UserRole.Student));
                return true; // Uspešno
            }
            catch (InvalidOperationException)
            {
                return false; // Odbijen
            }
        }));

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        // Očekujemo da je TAČNO JEDAN task uspeo.
        // Ako repozitorijum nije atomaran, više njih će uspeti i test će pasti.
        Assert.That(results.Count(success => success), Is.EqualTo(1));
        */
    }
}
