using System.Security.Cryptography;
using System.Text;
using Application.Sessions;

namespace Infrastructure.Sessions;

/// <summary>
/// Token: 32 nasumična bajta iz kriptografskog generatora (256 bita), zapisana tako da može u
/// HTTP zaglavlje. Heš: SHA-256. Za razliku od lozinke, token je dugačak i nasumičan, pa mu
/// nije potreban spor algoritam (PBKDF2), a brzo poređenje je potrebno pri svakom zahtevu.
/// </summary>
public sealed class SecureSessionTokenProvider : ISessionTokenProvider
{
    private const int TokenBytes = 32;

    public string Generate()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(TokenBytes);

        // Base64 bez oznaka koje smetaju u zaglavlju i adresi.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
