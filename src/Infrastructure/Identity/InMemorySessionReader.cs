using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Application.Identity;

namespace Infrastructure.Identity;


public sealed class InMemorySessionReader : ISessionReader
{
    private readonly ConcurrentDictionary<string, Guid> _userByTokenHash = new(StringComparer.Ordinal);

    public void Register(string sessionToken, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ArgumentException("Token sesije je obavezan.", nameof(sessionToken));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }

        _userByTokenHash[Hash(sessionToken)] = userId;
    }

    public bool Revoke(string sessionToken)
    {
        return !string.IsNullOrWhiteSpace(sessionToken)
            && _userByTokenHash.TryRemove(Hash(sessionToken), out _);
    }

    public Task<Guid?> FindUserIdAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return Task.FromResult<Guid?>(null);
        }

        return _userByTokenHash.TryGetValue(Hash(sessionToken), out var userId)
            ? Task.FromResult<Guid?>(userId)
            : Task.FromResult<Guid?>(null);
    }

    private static string Hash(string sessionToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionToken)));
    }
}
