using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Users;
using Domain.Users;

namespace Infrastructure.Users;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public InMemoryUserRepository(IEnumerable<User>? initialUsers = null)
    {
        if (initialUsers != null)
        {
            foreach (var user in initialUsers)
            {
                _users[user.Id] = user;
            }
        }
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult<User?>(null);
        }
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var user = _users.Values.FirstOrDefault(u => u.Username == normalizedUsername);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
