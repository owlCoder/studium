namespace Application.Home;


public interface IOpenTaskLinkReader
{
    Task<IReadOnlyList<OpenTaskLinkDto>> ListForStudentAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
