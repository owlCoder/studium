using System.Collections.Concurrent;
using Application.Sessions;
using Domain.Sessions;

namespace Infrastructure.Sessions;

/// <summary>
/// Memorijski adapter za <see cref="ISessionStore"/>, samo za demonstraciju i testove.
/// Sesije nestaju pri restartu aplikacije, a istekle se ne brišu (nema čišćenja); trajna
/// implementacija sa bazom ide uz trajnu bazu korisnika.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessionsByTokenHash = new();

    public Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_sessionsByTokenHash.TryAdd(session.TokenHash, session))
        {
            throw new InvalidOperationException("Sesija sa istim tokenom već postoji.");
        }

        return Task.CompletedTask;
    }

    public Task<Session?> FindByTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessionsByTokenHash.TryGetValue(tokenHash, out Session? session);
        return Task.FromResult(session);
    }
}
