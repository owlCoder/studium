namespace Application.Identity;


public interface IAccountStatusReader
{
    Task<bool> IsActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
