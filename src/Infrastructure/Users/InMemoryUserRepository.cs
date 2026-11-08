using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Users;
using Domain.Users;

namespace Infrastructure.Users;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, User> _usersById = new();
    private readonly Dictionary<string, Guid> _idsByUsername = new();

    public InMemoryUserRepository(IEnumerable<User>? initialUsers = null)
    {
        if (initialUsers == null)
        {
            return;
        }
        foreach (var user in initialUsers)
        {
            AddCore(user);
        }
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _usersById.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult<User?>(null);
        }
        var normalized = username.Trim().ToLowerInvariant();

        lock (_gate)
        {
            if (_idsByUsername.TryGetValue(normalized, out var id))
            {
                return Task.FromResult<User?>(_usersById[id]);
            }
            return Task.FromResult<User?>(null);
        }
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            AddCore(user);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            if (!_usersById.TryGetValue(user.Id, out var existing))
            {
                throw new InvalidOperationException("Korisnik ne postoji.");
            }
            if (_idsByUsername.TryGetValue(user.Username, out var ownerId) && ownerId != user.Id)
            {
                throw new InvalidOperationException("Korisničko ime je već zauzeto.");
            }
            _idsByUsername.Remove(existing.Username);
            _usersById[user.Id] = user;
            _idsByUsername[user.Username] = user.Id;
        }

        return Task.CompletedTask;
    }

    // Poziva se samo unutar lock-a (ili iz konstruktora).
    private void AddCore(User user)
    {
        if (_usersById.ContainsKey(user.Id))
        {
            throw new InvalidOperationException("Korisnik sa istim ID-em već postoji.");
        }
        if (_idsByUsername.ContainsKey(user.Username))
        {
            throw new InvalidOperationException("Korisničko ime je već zauzeto.");
        }
        _usersById[user.Id] = user;
        _idsByUsername[user.Username] = user.Id;
    }
}
