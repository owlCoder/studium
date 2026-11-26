using Domain.Users;

namespace Application.Users;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Username,
    UserRole Role,
    string? Email = null
);
