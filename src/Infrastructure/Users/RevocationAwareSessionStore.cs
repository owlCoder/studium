using Application.Sessions;
using Domain.Sessions;

namespace Infrastructure.Users;

/// <summary>
/// Dekorator nad <see cref="ISessionStore"/>: sesija izdata pre poslednje odjave korisnika
/// smatra se poništenom, pa je skladište ne vraća. Tako ResolveSessionHandler odbija staru
/// sesiju pri svakom zahtevu, bez izmene postojećeg skladišta sesija.
/// </summary>
public sealed class RevocationAwareSessionStore : ISessionStore
{
    private readonly ISessionStore _inner;
    private readonly InMemorySessionRevoker _revoker;

    public RevocationAwareSessionStore(ISessionStore inner, InMemorySessionRevoker revoker)
    {
        _inner = inner;
        _revoker = revoker;
    }

    public Task AddAsync(Session session, CancellationToken cancellationToken = default)
        => _inner.AddAsync(session, cancellationToken);

    public async Task<Session?> FindByTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var session = await _inner.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (session is null || _revoker.IsRevoked(session.UserId, session.CreatedAt))
            return null;

        return session;
    }
}
