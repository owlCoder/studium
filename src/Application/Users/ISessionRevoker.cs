
namespace Application.Users;

public interface ISessionRevoker
{
    Task RevokeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
