using Application.Sessions;

namespace Application.Users;

public sealed class ChangePasswordHandler
{
    // TODO: uskladiti sa specifikacijom (zahtev o lozinkama).
    private const int MinPasswordLength = 8;

    private readonly IPasswordAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionRevoker _sessionRevoker;
    private readonly ISessionTokenProvider _tokens;

    public ChangePasswordHandler(
        IPasswordAccountRepository accounts,
        IPasswordHasher passwordHasher,
        ISessionRevoker sessionRevoker,
        ISessionTokenProvider tokens)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _sessionRevoker = sessionRevoker;
        _tokens = tokens;
    }

    /// <param name="currentToken">Token sesije sa koje se lozinka menja; ta sesija ostaje važeća.</param>
    public async Task<ChangePasswordResult> HandleAsync(
        Guid authenticatedUserId,
        string currentToken,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty || string.IsNullOrWhiteSpace(currentToken))
            return ChangePasswordResult.AccountNotFound;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
            return ChangePasswordResult.InvalidNewPassword;

        var account = await _accounts.GetAccountAsync(
            authenticatedUserId,
            cancellationToken);

        if (account is null)
            return ChangePasswordResult.AccountNotFound;

        // ST-002: deaktiviran nalog ne sme da koristi zaštićene funkcije.
        // Proverava se pre lozinke, da se ne otkriva da li je lozinka tačna.
        if (!account.IsActive)
            return ChangePasswordResult.AccountInactive;

        if (string.IsNullOrEmpty(currentPassword)
            || !_passwordHasher.Verify(currentPassword, account.PasswordHash))
            return ChangePasswordResult.InvalidCurrentPassword;

        // Nova lozinka mora da se razlikuje od postojeće.
        if (newPassword == currentPassword)
            return ChangePasswordResult.InvalidNewPassword;

        await _accounts.UpdatePasswordHashAsync(
            authenticatedUserId,
            _passwordHasher.Hash(newPassword),
            cancellationToken);

        // Posle promene lozinke prestaju da važe sve ostale sesije korisnika.
        await _sessionRevoker.RevokeOtherSessionsAsync(
            authenticatedUserId,
            _tokens.Hash(currentToken),
            cancellationToken);

        return ChangePasswordResult.Success;
    }
}
