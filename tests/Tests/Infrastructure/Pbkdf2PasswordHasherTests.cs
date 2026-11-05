using NUnit.Framework;
using Infrastructure.Security;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class Pbkdf2PasswordHasherTests
{
    // Mali broj iteracija samo zbog brzine testova; produkcija koristi DefaultIterations.
    private static Pbkdf2PasswordHasher CreateHasher()
    {
        return new Pbkdf2PasswordHasher(1000);
    }

    [Test]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hasher = CreateHasher();
        string hash = hasher.Hash("Tajna123!");

        Assert.That(hasher.Verify("Tajna123!", hash), Is.True);
    }

    [Test]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hasher = CreateHasher();
        string hash = hasher.Hash("Tajna123!");

        Assert.That(hasher.Verify("tajna123!", hash), Is.False);
    }

    [Test]
    public void Hash_ReturnsDifferentHashes_ForSameInput()
    {
        var hasher = CreateHasher();

        string first = hasher.Hash("Tajna123!");
        string second = hasher.Hash("Tajna123!");

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Hash_DoesNotContainPlainPassword()
    {
        var hasher = CreateHasher();

        string hash = hasher.Hash("Tajna123!");

        Assert.That(hash, Does.Not.Contain("Tajna123!"));
    }

    [Test]
    public void Verify_StillWorks_WhenHashWasMadeWithDifferentIterationCount()
    {
        string oldHash = new Pbkdf2PasswordHasher(500).Hash("Tajna123!");

        Assert.That(new Pbkdf2PasswordHasher(2000).Verify("Tajna123!", oldHash), Is.True);
    }

    [TestCase("")]
    [TestCase("nije-hes")]
    [TestCase("PBKDF2-SHA256$abc$x$y")]
    [TestCase("PBKDF2-SHA256$1000$%%%$%%%")]
    [TestCase("DRUGI$1000$AAAA$AAAA")]
    public void Verify_ReturnsFalse_ForMalformedHash(string storedHash)
    {
        var hasher = CreateHasher();

        Assert.That(hasher.Verify("Tajna123!", storedHash), Is.False);
    }

    [Test]
    public void Verify_ReturnsFalse_ForEmptyPassword()
    {
        var hasher = CreateHasher();
        string hash = hasher.Hash("Tajna123!");

        Assert.That(hasher.Verify("", hash), Is.False);
    }

    [TestCase("")]
    public void Hash_RejectsEmptyPassword(string password)
    {
        var hasher = CreateHasher();

        Assert.That(
            () => hasher.Hash(password),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsNonPositiveIterations()
    {
        Assert.That(
            () => new Pbkdf2PasswordHasher(0),
            Throws.TypeOf<ArgumentException>());
    }
}
