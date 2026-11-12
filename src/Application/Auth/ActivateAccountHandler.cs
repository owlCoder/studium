using Application.Users;
using Domain.Users;

namespace Application.Auth;

public sealed class ActivateAccountHandler
{
    private readonly IUserLookup _users;
    private readonly IPasswordHasher _passwordHasher;

    public ActivateAccountHandler(
        IUserLookup users,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> HandleAsync(ActivateAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) ||
            string.IsNullOrWhiteSpace(command.Token) ||
            string.IsNullOrWhiteSpace(command.Password))
        {
            return false;
        }

        string normalized = User.NormalizeUsername(command.Username);
        User? user = await _users.FindByUsernameAsync(normalized, cancellationToken);

        if (user is null)
        {
            return false;
        }

        // Provera tokena i roka trajanja
        if (string.IsNullOrEmpty(user.ActivationToken) || user.ActivationToken != command.Token)
        {
            return false;
        }

        if (user.TokenExpiration.HasValue && user.TokenExpiration.Value < DateTime.UtcNow)
        {
            return false;
        }

        // Heširanje i izvršavanje aktivacije kroz domen model
        string passwordHash = _passwordHasher.Hash(command.Password);
        user.Activate(passwordHash, command.Token);

        return true;
    }
}
