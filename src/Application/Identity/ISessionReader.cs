namespace Application.Identity;


public interface ISessionReader
{
    Task<Guid?> FindUserIdAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);
}
