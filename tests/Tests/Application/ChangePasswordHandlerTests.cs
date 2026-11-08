using Application.Users;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class ChangePasswordHandlerTests
{
    private IPasswordAccountRepository _accounts = null!;
    private IPasswordHasher _hasher = null!;
    private ChangePasswordHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = Substitute.For<IPasswordAccountRepository>();
        _hasher = Substitute.For<IPasswordHasher>();

        _handler = new ChangePasswordHandler(_accounts, _hasher);
    }

    [Test]
    public async Task HandleAsync_ReturnsSuccess_WhenPasswordIsCorrect()
    {
        var userId = Guid.NewGuid();

        _accounts.GetPasswordHashAsync(userId, Arg.Any<CancellationToken>())
            .Returns("old-hash");

        _hasher.Verify("Stara123!", "old-hash").Returns(true);
        _hasher.Hash("Nova123!").Returns("new-hash");

        var result = await _handler.HandleAsync(
            userId, "Stara123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.Success));

        await _accounts.Received(1).UpdatePasswordHashAsync(
            userId, "new-hash", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidCurrentPassword_WhenOldPasswordIsWrong()
    {
        var userId = Guid.NewGuid();

        _accounts.GetPasswordHashAsync(userId, Arg.Any<CancellationToken>())
            .Returns("old-hash");

        _hasher.Verify("Pogresna123!", "old-hash").Returns(false);

        var result = await _handler.HandleAsync(
            userId, "Pogresna123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidCurrentPassword));

        await _accounts.DidNotReceive().UpdatePasswordHashAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _accounts.GetPasswordHashAsync(userId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _handler.HandleAsync(
            userId, "Stara123!", "Nova123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.AccountNotFound));
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidNewPassword_WhenNewPasswordIsEmpty()
    {
        var result = await _handler.HandleAsync(
            Guid.NewGuid(), "Stara123!", "");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidNewPassword));
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidNewPassword_WhenNewPasswordIsTooShort()
    {
        var result = await _handler.HandleAsync(
            Guid.NewGuid(), "Stara123!", "Ab1!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidNewPassword));

        await _accounts.DidNotReceive().UpdatePasswordHashAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidNewPassword_WhenNewPasswordEqualsCurrent()
    {
        var userId = Guid.NewGuid();

        _accounts.GetPasswordHashAsync(userId, Arg.Any<CancellationToken>())
            .Returns("old-hash");

        _hasher.Verify("Stara123!", "old-hash").Returns(true);

        var result = await _handler.HandleAsync(
            userId, "Stara123!", "Stara123!");

        Assert.That(result, Is.EqualTo(ChangePasswordResult.InvalidNewPassword));

        await _accounts.DidNotReceive().UpdatePasswordHashAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}