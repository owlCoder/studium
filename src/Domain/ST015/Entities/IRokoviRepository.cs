using Studium.Domain.ST015.Entities;

namespace Studium.Domain.ST015.Repositories;

public interface IRokoviRepository
{
    Task<Zadatak?> GetZadatakAsync(Guid zadatakId, CancellationToken cancellationToken);
    Task<StudentZadatakRok?> GetStudentRokAsync(Guid studentId, Guid zadatakId, CancellationToken cancellationToken);
    Task UpdateStudentRokAsync(StudentZadatakRok studentRok);
    Task<bool> DaLiNastavnikImaPravoNadZadatkomAsync(Guid nastavnikId, Guid zadatakId, CancellationToken cancellationToken = default);
}