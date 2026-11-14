using Studium.Domain.ST015.Entities;
using Studium.Domain.ST015.Repositories;

namespace Studium.Infrastructure.ST015;

/// <summary>
/// Eksplicitni fail-closed KT1 adapter za IRokoviRepository dok trajna baza podataka nije povezana.
/// Podrazumevano odbija ovlašćenja i ne pronalazi zapise bez rušenja DI kontejnera.
/// </summary>
public sealed class FailClosedRokoviRepository : IRokoviRepository
{
    public Task<StudentZadatakRok?> GetStudentRokAsync(Guid studentId, Guid zadatakId, CancellationToken ct = default)
        => Task.FromResult<StudentZadatakRok?>(null);

    public Task UpdateStudentRokAsync(StudentZadatakRok studentRok, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<Zadatak?> GetZadatakAsync(Guid zadatakId, CancellationToken ct = default)
        => Task.FromResult<Zadatak?>(null);

    public Task<bool> DaLiNastavnikImaPravoNadZadatkomAsync(Guid nastavnikId, Guid zadatakId, CancellationToken ct = default)
        => Task.FromResult(false);
}