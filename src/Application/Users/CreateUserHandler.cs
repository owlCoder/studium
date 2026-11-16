using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Users;

namespace Application.Users;

public sealed class CreateUserHandler
{
    private readonly IUserRepository _userRepository;

    public CreateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = command.Username.Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"Unos {command.Username} se odbija kao duplikat naloga.");
        }

        var user = new User(
            Guid.NewGuid(),
            command.FirstName,
            command.LastName,
            command.Username,
            command.Role
        );

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            user.UpdateEmail(command.Email);
        }

        await _userRepository.AddAsync(user, cancellationToken);
        return user;
    }
}
