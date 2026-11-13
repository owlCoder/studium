namespace Application.Staff;

public enum UserRole
{
    Administrator,
    Teacher,
    Assistant,
    Student
}

public sealed record UserInfo(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive);

public interface IUserDirectory
{
    Task<UserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
