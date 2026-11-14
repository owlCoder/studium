using Application.Sessions;
using Application.Users;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class LogoutHandlerTests
{
    private ISessionRevoker _sessionRevoker = null!;
    private ISessionTokenProvider _tokens = null!;
    private LogoutHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _sessionRevoker = Substitute.For<ISessionRevoker>();
        _tokens = Substitute.For<ISessionTokenProvider>();
        _tokens.Hash("token-1").Returns("hash-1");

        _handler = new LogoutHandler(_sessionRevoker, _tokens);
    }

    [Test]
    public async Task HandleAsync_RevokesOnlyTheSessionOfTheGivenToken()
    {
        await _handler.HandleAsync("token-1");

        await _sessionRevoker.Received(1).RevokeSessionAsync(
            "hash-1",
            Arg.Any<CancellationToken>());

        await _sessionRevoker.DidNotReceive().RevokeOtherSessionsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void HandleAsync_ThrowsUnauthorizedAccessException_WhenTokenIsMissing(string? token)
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _handler.HandleAsync(token));
    }
}
