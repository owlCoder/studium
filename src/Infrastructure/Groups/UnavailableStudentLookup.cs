using Application.Groups;

namespace Infrastructure.Groups;

/// <summary>
/// Odbijajuca (fail-closed) zamena dok ne postoji stvarni izvor podataka o studentima.
/// Nikada ne tvrdi da je student aktivan, pa dodavanje u grupu ne prolazi bez prave provere.
/// </summary>
public sealed class UnavailableStudentLookup : IStudentLookup
{
    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
