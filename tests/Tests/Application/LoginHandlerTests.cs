using Moq;
using NUnit.Framework;
using Application.Auth;
using Application.Users;
using Domain.Users;

namespace Tests.Application;

/// <summary>
/// Testovi pravila ST-002 "Prijava". Portovi se zamenjuju Moq objektima, pa test proverava
/// samo ponašanje handler-a, bez baze i bez stvarnog heširanja.
/// </summary>
[TestFixture]
public sealed class LoginHandlerTests
{
    private const string Hash = "hes-ane";
    private const string DummyHash = "lazni-hes";
    private const string Password = "Sintetika-1";

    private static User AnaWith(bool isActive = true, string? hash = Hash) =>
        new(Guid.NewGuid(), "Ana.Petrovic", UserRole.Student, hash, isActive);

    /// <summary>Pravi handler; <paramref name="passwordMatches"/> određuje ishod provere lozinke.</summary>
    private static (LoginHandler Handler, Mock<IUserLookup> Users, Mock<IPasswordHasher> Hasher) Create(
        User? existingUser, bool passwordMatches = true)
    {
        var users = new Mock<IUserLookup>();
        users.Setup(x => x.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns(DummyHash);
        hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(passwordMatches);

        return (new LoginHandler(users.Object, hasher.Object), users, hasher);
    }

    // ---------- Uspeh ----------

    [Test]
    public async Task HandleAsync_ReturnsIdentity_WhenCredentialsAreValidAndAccountIsActive()
    {
        var user = AnaWith();
        var (handler, _, hasher) = Create(user);

        var result = await handler.HandleAsync(new LoginCommand("Ana.Petrovic", Password));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failure, Is.Null);
            Assert.That(result.Identity!.UserId, Is.EqualTo(user.Id));
            Assert.That(result.Identity.Username, Is.EqualTo("Ana.Petrovic"));
            Assert.That(result.Identity.Role, Is.EqualTo(UserRole.Student));
        });
        hasher.Verify(x => x.Verify(Password, Hash), Times.Once);
    }

    [TestCase("Ana.Petrovic")]
    [TestCase("ANA.PETROVIC")]
    [TestCase("  ana.petrovic  ")]
    public async Task HandleAsync_LooksUpUsername_WithoutRegardToCase(string typedUsername)
    {
        var (handler, users, _) = Create(AnaWith());

        await handler.HandleAsync(new LoginCommand(typedUsername, Password));

        // Port uvek dobija isti normalizovan oblik, pa je "Ana.Petrovic" isto što i "ana.petrovic".
        users.Verify(
            x => x.FindByUsernameAsync("ana.petrovic", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---------- Ista poruka za nepoznato ime i pogrešnu lozinku ----------

    [Test]
    public async Task HandleAsync_RejectsUnknownUsername_WithInvalidCredentials()
    {
        var (handler, _, hasher) = Create(existingUser: null);

        var result = await handler.HandleAsync(new LoginCommand("nepoznat", Password));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(LoginFailure.InvalidCredentials));
            Assert.That(result.Identity, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_VerifiesAgainstDummyHash_WhenUserDoesNotExist()
    {
        var (handler, _, hasher) = Create(existingUser: null);

        await handler.HandleAsync(new LoginCommand("nepoznat", Password));

        // Skupa provera se izvršava i za nepoznato ime, da vreme odgovora ne odaje nalog.
        hasher.Verify(x => x.Verify(Password, DummyHash), Times.Once);
    }

    [Test]
    public async Task HandleAsync_RejectsUnknownUsername_EvenIfDummyHashWouldMatch()
    {
        // Mock potvrđuje svaku lozinku (passwordMatches: true), a nalog ipak mora biti odbijen.
        var (handler, _, _) = Create(existingUser: null, passwordMatches: true);

        var result = await handler.HandleAsync(new LoginCommand("nepoznat", Password));

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_CreatesDummyHashOnce_NotPerRequest()
    {
        var (handler, _, hasher) = Create(existingUser: null);

        await handler.HandleAsync(new LoginCommand("nepoznat", Password));
        await handler.HandleAsync(new LoginCommand("nepoznat", Password));

        // Lažni heš se pravi u konstruktoru jednom, ne po zahtevu.
        hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_RejectsWrongPassword_WithInvalidCredentials()
    {
        var (handler, _, _) = Create(AnaWith(), passwordMatches: false);

        var result = await handler.HandleAsync(new LoginCommand("Ana.Petrovic", "pogresna"));

        Assert.That(result.Failure, Is.EqualTo(LoginFailure.InvalidCredentials));
    }

    [Test]
    public async Task HandleAsync_GivesSameFailure_ForUnknownUsernameAndWrongPassword()
    {
        var (unknownUser, _, _) = Create(existingUser: null);
        var (wrongPassword, _, _) = Create(AnaWith(), passwordMatches: false);

        var first = await unknownUser.HandleAsync(new LoginCommand("nepoznat", Password));
        var second = await wrongPassword.HandleAsync(new LoginCommand("Ana.Petrovic", "pogresna"));

        // Odgovor ne sme otkriti koja korisnička imena postoje.
        Assert.That(first.Failure, Is.EqualTo(second.Failure));
    }

    // ---------- Neaktivirani i deaktivirani nalozi ----------

    [Test]
    public async Task HandleAsync_RejectsAccount_WhoseActivationIsNotFinished()
    {
        var (handler, _, hasher) = Create(AnaWith(hash: null));

        var result = await handler.HandleAsync(new LoginCommand("Ana.Petrovic", Password));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            // Nalog bez lozinke ponaša se kao nepoznat, pa se ne otkriva da postoji.
            Assert.That(result.Failure, Is.EqualTo(LoginFailure.InvalidCredentials));
        });
        // Isto kao za nepoznat nalog: provera se izvršava protiv lažnog heša.
        hasher.Verify(x => x.Verify(Password, DummyHash), Times.Once);
    }

    [Test]
    public async Task HandleAsync_RejectsDeactivatedAccount_WhenPasswordIsCorrect()
    {
        var (handler, _, _) = Create(AnaWith(isActive: false));

        var result = await handler.HandleAsync(new LoginCommand("Ana.Petrovic", Password));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(LoginFailure.InvalidCredentials));
            Assert.That(result.Identity, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_GivesSameFailure_ForDeactivatedAccountAndWrongPassword()
    {
        var (deactivated, _, _) = Create(AnaWith(isActive: false));
        var (wrongPassword, _, _) = Create(AnaWith(), passwordMatches: false);

        var first = await deactivated.HandleAsync(new LoginCommand("Ana.Petrovic", Password));
        var second = await wrongPassword.HandleAsync(new LoginCommand("Ana.Petrovic", "pogresna"));

        // Odgovor ne otkriva da li je nalog deaktiviran.
        Assert.That(first.Failure, Is.EqualTo(second.Failure));
    }

    // ---------- Neispravan unos ----------

    [TestCase(null, Password)]
    [TestCase("", Password)]
    [TestCase("   ", Password)]
    [TestCase("Ana.Petrovic", null)]
    [TestCase("Ana.Petrovic", "")]
    public async Task HandleAsync_RejectsBlankInput_WithoutTouchingPorts(
        string? username, string? password)
    {
        var (handler, users, hasher) = Create(AnaWith());

        var result = await handler.HandleAsync(new LoginCommand(username, password));

        Assert.That(result.Failure, Is.EqualTo(LoginFailure.InvalidInput));
        users.Verify(
            x => x.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- Tehnička greška ----------

    [Test]
    public void HandleAsync_DoesNotSwallowLookupFailure()
    {
        var users = new Mock<IUserLookup>();
        users.Setup(x => x.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Izvor podataka nije dostupan."));
        var handler = new LoginHandler(users.Object, Mock.Of<IPasswordHasher>());

        // Tehnička greška ne sme da izgleda kao "pogrešna lozinka".
        Assert.That(
            async () => await handler.HandleAsync(new LoginCommand("Ana.Petrovic", Password)),
            Throws.TypeOf<InvalidOperationException>());
    }
}
