using Domain.Users;

namespace Application.Access;

public sealed record AccountInfo(
    Guid Id,
    UserRole Role,
    bool IsActive,
    bool IsActivated);
