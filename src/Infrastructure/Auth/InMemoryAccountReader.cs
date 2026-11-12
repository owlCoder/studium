using Application.Access;
using Domain.Users;

namespace Infrastructure.Auth;

/// <summary>
/// Memorijski adapter  samo za demonstraciju i testove.
/// Stanje naloga čita iz istih korisnika iz kojih se prijavljuje ,
/// pa prijava i provera ovlašćenja ne mogu da se razilaze.
/// </summary>
public sealed class InMemoryAccountReader : IAccountReader
{
    private readonly IReadOnlyDictionary<Guid, User> _usersById;

    public InMemoryAccountReader(IEnumerable<User> users)
    {
        _usersById = users.ToDictionary(u => u.Id);
    }

    public Task<AccountInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_usersById.TryGetValue(id, out User? user))
        {
            return Task.FromResult<AccountInfo?>(null);
        }

        var account = new AccountInfo(user.Id, user.Role, user.IsActive, user.IsActivated);
        return Task.FromResult<AccountInfo?>(account);
    }
}
