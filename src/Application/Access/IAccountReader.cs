namespace Application.Access;

public interface IAccountReader
{
    Task<AccountInfo?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
