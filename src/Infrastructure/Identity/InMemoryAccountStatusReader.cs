using System.Collections.Concurrent;
using Application.Identity;

namespace Infrastructure.Identity;


public sealed class InMemoryAccountStatusReader : IAccountStatusReader
{
    private readonly ConcurrentDictionary<Guid, bool> _isActiveByUser = new();

    public void Activate(Guid userId)
    {
        EnsureUser(userId);
        _isActiveByUser[userId] = true;
    }

    public void Deactivate(Guid userId)
    {
        EnsureUser(userId);
        _isActiveByUser[userId] = false;
    }

    public Task<bool> IsActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_isActiveByUser.TryGetValue(userId, out var isActive) && isActive);
    }

    private static void EnsureUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }
    }
}
