using System.Collections.Concurrent;
using Application.Time;
using Application.Users;

namespace Infrastructure.Users;

/// <summary>
/// Memorijski adapter za <see cref="ISessionRevoker"/>. Pamti vreme poslednje odjave po korisniku;
/// sesija/token izdat pre tog trenutka se smatra poništenim.
/// </summary>
public sealed class InMemorySessionRevoker : ISessionRevoker
{
    private readonly IClock _clock;
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _revokedAt = new();

    public InMemorySessionRevoker(IClock clock)
    {
        _clock = clock;
    }

    public Task RevokeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _revokedAt[userId] = _clock.UtcNow;
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetLastRevokedAt(Guid userId)
        => _revokedAt.TryGetValue(userId, out var at) ? at : null;

    /// <summary>Da li je sesija/token izdat u <paramref name="issuedAt"/> poništen odjavom.</summary>
    public bool IsRevoked(Guid userId, DateTimeOffset issuedAt)
        => _revokedAt.TryGetValue(userId, out var at) && issuedAt <= at;
}