
using Application.Users;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class LogoutHandlerTests
{
    private ISessionRevoker _sessionRevoker = null!;
    private LogoutHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _sessionRevoker = Substitute.For<ISessionRevoker>();
        _handler = new LogoutHandler(_sessionRevoker);
    }

    [Test]
    public async Task HandleAsync_RevokesSession_WhenUserIsAuthenticated()
    {
        var userId = Guid.NewGuid();

        await _handler.HandleAsync(userId);

        await _sessionRevoker.Received(1).RevokeAsync(
            userId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void HandleAsync_ThrowsUnauthorizedAccessException_WhenUserIdIsEmpty()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _handler.HandleAsync(Guid.Empty));
    }
}
