using Domain.Sessions;

namespace Application.Sessions;

/// <summary>
/// Port: čuvanje i pronalaženje sesija.
/// </summary>
public interface ISessionStore
{
    Task AddAsync(Session session, CancellationToken cancellationToken = default);

    /// <returns>Sesija ili <c>null</c> ako ne postoji.</returns>
    Task<Session?> FindByTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default);
}
