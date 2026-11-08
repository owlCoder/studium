namespace Application.Users;

public interface IPasswordAccountRepository
{
    Task<string?> GetPasswordHashAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task UpdatePasswordHashAsync(
        Guid userId,
        string newPasswordHash,
        CancellationToken cancellationToken = default);
}