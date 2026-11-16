using Studium.Domain.ST015.Entities;

namespace Studium.Domain.ST015.Repositories;

public interface IRokoviRepository
{
    Task<StudentZadatakRok?> GetStudentRokAsync(Guid studentId, Guid zadatakId, CancellationToken ct = default);
    Task UpdateStudentRokAsync(StudentZadatakRok studentRok, CancellationToken ct = default);
    Task<Zadatak?> GetZadatakAsync(Guid zadatakId, CancellationToken ct = default);
    Task<bool> DaLiNastavnikImaPravoNadZadatkomAsync(Guid nastavnikId, Guid zadatakId, CancellationToken ct = default);
}