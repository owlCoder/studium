using Domain.Users;

namespace Application.Auth;

public interface IUserLookup
{
    Task<User?> FindByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default);
}
