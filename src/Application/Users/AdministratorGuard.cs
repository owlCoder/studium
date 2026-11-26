using Domain.Users;

namespace Application.Users;

/// Akter nije identifikovan ili ne postoji. HTTP 401.
public sealed class ActorNotAuthenticatedException : Exception
{
    public ActorNotAuthenticatedException()
        : base("Akter nije identifikovan.") { }
}

/// Akter postoji, ali nije administrator. HTTP 403.
public sealed class ActorNotAuthorizedException : Exception
{
    public ActorNotAuthorizedException()
        : base("Samo administrator može da izvrši ovu radnju.") { }
}

/// Server-side provera da je akter administrator. Ne zavisi od tuđih fajlova.
public sealed class AdministratorGuard
{
    private readonly IUserRepository _users;

    public AdministratorGuard(IUserRepository users)
    {
        _users = users;
    }

    public async Task EnsureAdministratorAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        var actor = await _users.GetByIdAsync(actorId, cancellationToken);
        if (actor is null)
            throw new ActorNotAuthenticatedException();

        if (actor.Role != UserRole.Administrator)
            throw new ActorNotAuthorizedException();
    }
}
