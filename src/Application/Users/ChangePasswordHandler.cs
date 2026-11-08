namespace Application.Users;

public sealed class ChangePasswordHandler
{
    // TODO: uskladiti sa specifikacijom (zahtev o lozinkama).
    private const int MinPasswordLength = 8;

    private readonly IPasswordAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IPasswordAccountRepository accounts,
        IPasswordHasher passwordHasher)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResult> HandleAsync(
        Guid authenticatedUserId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty)
            return ChangePasswordResult.AccountNotFound;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
            return ChangePasswordResult.InvalidNewPassword;

        var currentHash = await _accounts.GetPasswordHashAsync(
            authenticatedUserId,
            cancellationToken);

        if (currentHash is null)
            return ChangePasswordResult.AccountNotFound;

        if (string.IsNullOrEmpty(currentPassword)
            || !_passwordHasher.Verify(currentPassword, currentHash))
            return ChangePasswordResult.InvalidCurrentPassword;

        // Nova lozinka mora da se razlikuje od postojeće.
        if (newPassword == currentPassword)
            return ChangePasswordResult.InvalidNewPassword;

        await _accounts.UpdatePasswordHashAsync(
            authenticatedUserId,
            _passwordHasher.Hash(newPassword),
            cancellationToken);

        return ChangePasswordResult.Success;
    }
}