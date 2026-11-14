using Application.Sessions;
using Application.Users;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class ChangePasswordHandlerTests
{
    private const string Token = "token-current";
    private const string TokenHash = "hash-current";

    private IPasswordAccountRepository _accounts = null!;
    private IPasswordHasher _hasher = null!;
    private ISessionRevoker _sessionRevoker = null!;
    private ISessionTokenProvider _tokens = null!;
    private ChangePasswordHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = Substitute.For<IPasswordAccountRepository>();
        _hasher = Substitute.For<IPasswordHasher>();
        _sessionRevoker = Substitute.For<ISessionRevoker>();
        _tokens = Substitute.For<ISessionTokenProvider>();
        _tokens.Hash(Token).Returns(TokenHash);

        _handler = new ChangePasswordHandler(_accounts, _hasher, _sessionRevoker, _tokens);
    }

    private void GivenAccount(Guid userId, bool isActive = true)
    {
        _accounts.GetAccountAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new PasswordAccount("old-hash", isActive));
    }

    private async Task AssertNothingChangedAsync()
    {
        await _accounts.DidNotReceive().UpdatePasswordHashAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _sessionRevoker.DidNotReceive().RevokeOtherSessionsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ReturnsSuccess_WhenPasswordIsCorrect()
    {
        var userId = Guid.NewGuid();
        GivenAccount(userId);

        _hasher.Verify("Stara123!", "old-hash").Returns(true);
        _hasher.Hash("Nova123!").Returns("new-hash");

        var result = await _handler.HandleAsync(
            userId, Token, "Stara123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.Success));

        await _accounts.Received(1).UpdatePasswordHashAsync(
            userId, "new-hash", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_RevokesOtherSessions_AndKeepsTheCurrentOne_OnSuccess()
    {
        var userId = Guid.NewGuid();
        GivenAccount(userId);

        _hasher.Verify("Stara123!", "old-hash").Returns(true);
        _hasher.Hash("Nova123!").Returns("new-hash");

        await _handler.HandleAsync(userId, Token, "Stara123!", "Nova123!");

        await _sessionRevoker.Received(1).RevokeOtherSessionsAsync(
            userId, TokenHash, Arg.Any<CancellationToken>());

        await _sessionRevoker.DidNotReceive().RevokeSessionAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidCurrentPassword_WhenOldPasswordIsWrong()
    {
        var userId = Guid.NewGuid();
        GivenAccount(userId);

        _hasher.Verify("Pogresna123!", "old-hash").Returns(false);

        var result = await _handler.HandleAsync(
            userId, Token, "Pogresna123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidCurrentPassword));
        await AssertNothingChangedAsync();
    }

    [Test]
    public async Task HandleAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _accounts.GetAccountAsync(userId, Arg.Any<CancellationToken>())
            .Returns((PasswordAccount?)null);

        var result = await _handler.HandleAsync(
            userId, Token, "Stara123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.AccountNotFound));
        await AssertNothingChangedAsync();
    }

    [Test]
    public async Task HandleAsync_ReturnsAccountNotFound_WhenTokenIsMissing()
    {
        var result = await _handler.HandleAsync(
            Guid.NewGuid(), "", "Stara123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.AccountNotFound));
        await AssertNothingChangedAsync();
    }

    [Test]
    public async Task HandleAsync_ReturnsAccountInactive_WhenAccountIsDeactivated()
    {
        var userId = Guid.NewGuid();
        GivenAccount(userId, isActive: false);

        _hasher.Verify("Stara123!", "old-hash").Returns(true);

        var result = await _handler.HandleAsync(
            userId, Token, "Stara123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.AccountInactive));
        await AssertNothingChangedAsync();
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidNewPassword_WhenNewPasswordIsEmpty()
    {
        var result = await _handler.HandleAsync(
            Guid.NewGuid(), Token, "Stara123!", "");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidNewPassword));
        await AssertNothingChangedAsync();
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidNewPassword_WhenNewPasswordIsTooShort()
    {
        var result = await _handler.HandleAsync(
            Guid.NewGuid(), Token, "Stara123!", "Ab1!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidNewPassword));
        await AssertNothingChangedAsync();
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidNewPassword_WhenNewPasswordEqualsCurrent()
    {
        var userId = Guid.NewGuid();
        GivenAccount(userId);

        _hasher.Verify("Stara123!", "old-hash").Returns(true);

        var result = await _handler.HandleAsync(
            userId, Token, "Stara123!", "Stara123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidNewPassword));
        await AssertNothingChangedAsync();
    }
}
