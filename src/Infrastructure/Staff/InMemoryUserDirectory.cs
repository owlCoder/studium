using Application.Staff;

namespace Infrastructure.Staff;

public sealed class InMemoryUserDirectory : IUserDirectory
{
    private readonly Dictionary<Guid, UserInfo> _users = new();

    public void Add(UserInfo user) => _users[user.Id] = user;

    public Task<UserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);
    }
}
