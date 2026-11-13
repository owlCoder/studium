using Domain.Works;

namespace Application.Works;

public interface IStudentWorkRepository
{
    /// <summary>
    /// Vraća SVE radove studenta — bez obzira na status članstva.
    /// Ključno: prekinut student vidi svoju istoriju.
    /// </summary>
    Task<IReadOnlyList<StudentWork>> GetByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
