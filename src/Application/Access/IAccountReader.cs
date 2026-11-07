namespace Application.Access;

public interface IAccountReader
{
    /// <summary>Vraća null ako nalog ne postoji.</summary>
    Task<AccountInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
