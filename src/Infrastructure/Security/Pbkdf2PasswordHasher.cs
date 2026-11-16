using System.Security.Cryptography;
using Application.Users;

namespace Infrastructure.Security;

/// <summary>
/// Heširanje lozinki algoritmom PBKDF2-HMAC-SHA256 sa nasumičnom solju po lozinci.
/// Format zapisa: PBKDF2-SHA256$iteracije$so(base64)$heš(base64).
/// Broj iteracija je upisan u sam heš, pa se kasnije može povećati bez prekida starih naloga.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int DefaultIterations = 600000;

    private const string Prefix = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly int _iterations;

    public Pbkdf2PasswordHasher()
        : this(DefaultIterations)
    {
    }

    public Pbkdf2PasswordHasher(int iterations)
    {
        if (iterations < 1)
        {
            throw new ArgumentException("Broj iteracija mora biti pozitivan.", nameof(iterations));
        }

        _iterations = iterations;
    }

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Lozinka je obavezna.", nameof(password));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, _iterations, HashAlgorithmName.SHA256, HashSize);

        return Prefix
            + "$" + _iterations.ToString()
            + "$" + Convert.ToBase64String(salt)
            + "$" + Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        string[] parts = storedHash.Split('$');
        if (parts.Length != 4)
        {
            return false;
        }

        if (parts[0] != Prefix)
        {
            return false;
        }

        int iterations;
        if (!int.TryParse(parts[1], out iterations) || iterations < 1)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || expected.Length == 0)
        {
            return false;
        }

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
