namespace Application.Users;
using Domain.Users;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Username,
    UserRole Role,
    string? Email = null
);
