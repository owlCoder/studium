using Application.Auth;
using Domain.Users;

namespace Infrastructure.Auth;

/// <summary>
/// Memorijski adapter za <see cref="IUserLookup"/>, samo za demonstraciju i testove.
/// </summary>
public sealed class InMemoryUserLookup : IUserLookup
{
    private readonly IReadOnlyDictionary<string, User> _usersByUsername;

    public InMemoryUserLookup(IEnumerable<User> users)
    {
        // ToDictionary baca izuzetak za duplikat, što odgovara pravilu jedinstvenog imena.
        _usersByUsername = users.ToDictionary(u => User.NormalizeUsername(u.Username));
    }

    public Task<User?> FindByUsernameAsync(string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _usersByUsername.TryGetValue(normalizedUsername, out User? user);
        return Task.FromResult(user);
    }
}
