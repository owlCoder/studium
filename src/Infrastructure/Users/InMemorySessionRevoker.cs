using System.Collections.Concurrent;
using Application.Time;
using Application.Users;

namespace Infrastructure.Users;

/// <summary>
/// Memorijski adapter za <see cref="ISessionRevoker"/>.
/// Odjava pamti heš opozvanog tokena. Promena lozinke pamti trenutak i heš sesije koja se
/// zadržava: sve druge sesije korisnika nastale do tog trenutka se smatraju poništenim.
/// </summary>
public sealed class InMemorySessionRevoker : ISessionRevoker
{
    private sealed record Cutoff(DateTimeOffset At, string KeepTokenHash);

    private readonly IClock _clock;
    private readonly ConcurrentDictionary<string, byte> _revokedTokens = new();
    private readonly ConcurrentDictionary<Guid, Cutoff> _cutoffs = new();

    public InMemorySessionRevoker(IClock clock)
    {
        _clock = clock;
    }

    public Task RevokeSessionAsync(string tokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Heš tokena je obavezan.", nameof(tokenHash));

        _revokedTokens[tokenHash] = 0;
        return Task.CompletedTask;
    }

    public Task RevokeOtherSessionsAsync(Guid userId, string keepTokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == Guid.Empty)
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        if (string.IsNullOrWhiteSpace(keepTokenHash))
            throw new ArgumentException("Heš tokena je obavezan.", nameof(keepTokenHash));

        // Poslednja promena lozinke važi: svaka starija sesija osim zadržane je poništena.
        _cutoffs[userId] = new Cutoff(_clock.UtcNow, keepTokenHash);
        return Task.CompletedTask;
    }

    /// <summary>Da li je sesija sa datim hešom tokena poništena odjavom ili promenom lozinke.</summary>
    public bool IsRevoked(string tokenHash, Guid userId, DateTimeOffset createdAt)
    {
        if (_revokedTokens.ContainsKey(tokenHash))
            return true;

        return _cutoffs.TryGetValue(userId, out var cutoff)
            && tokenHash != cutoff.KeepTokenHash
            && createdAt <= cutoff.At;
    }
}
