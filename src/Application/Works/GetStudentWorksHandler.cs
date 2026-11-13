using Domain.Works;

namespace Application.Works;

/// <summary>
/// Vraća istoriju radova studenta — radi i za studente sa prekinutim članstvom.
/// </summary>
public sealed class GetStudentWorksHandler
{
    private readonly IStudentWorkRepository _repository;

    public GetStudentWorksHandler(IStudentWorkRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<StudentWork>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Identifikator studenta mora biti zadat.", nameof(studentId));

        return await _repository.GetByStudentAsync(studentId, cancellationToken);
    }
}
