namespace Application.Users;

/// <summary>Podaci o nalogu potrebni za promenu lozinke.</summary>
public sealed record PasswordAccount(string PasswordHash, bool IsActive);

public interface IPasswordAccountRepository
{
    /// <summary>
    /// Vraća nalog ili null ako ne postoji ili aktivacija nije završena (nema lozinku).
    /// </summary>
    Task<PasswordAccount?> GetAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task UpdatePasswordHashAsync(
        Guid userId,
        string newPasswordHash,
        CancellationToken cancellationToken = default);
}
