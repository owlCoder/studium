using Application.Auth;
using Application.Users;
using Domain.Users;

namespace Infrastructure.Users;

/// <summary>
/// Memorijski adapter, samo za demonstraciju i testove. Deli skup naloga između prijave
/// (IUserLookup) i promene lozinke (IPasswordAccountRepository).
/// </summary>
public sealed class InMemoryUserStore : IUserLookup, IPasswordAccountRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, User> _usersById;

    public InMemoryUserStore(IEnumerable<User> users)
    {
        var list = users.ToList();
        var names = list.Select(u => User.NormalizeUsername(u.Username)).ToList();
        if (names.Distinct().Count() != names.Count)
            throw new ArgumentException("Korisnička imena moraju biti jedinstvena.", nameof(users));

        _usersById = list.ToDictionary(u => u.Id);
    }

    public Task<User?> FindByUsernameAsync(string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var user = _usersById.Values.FirstOrDefault(
                u => User.NormalizeUsername(u.Username) == normalizedUsername);
            return Task.FromResult(user);
        }
    }

    public Task<string?> GetPasswordHashAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user?.PasswordHash);
        }
    }

    public Task UpdatePasswordHashAsync(Guid userId, string newPasswordHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_usersById.TryGetValue(userId, out var user))
                throw new InvalidOperationException("Nalog ne postoji.");

            _usersById[userId] = new User(
                user.Id, user.Username, user.Role, newPasswordHash, user.IsActive);
        }
        return Task.CompletedTask;
    }
}