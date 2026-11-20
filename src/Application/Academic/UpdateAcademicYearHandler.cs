using Domain.Academic;

namespace Application.Academic;

public sealed class UpdateAcademicYearHandler
{
    private readonly IAcademicYearRepository _repository;

    public UpdateAcademicYearHandler(IAcademicYearRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(
        UpdateAcademicYearCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new InvalidOperationException("Akademska godina ne postoji.");

        // Konstruktor domena ponovo proverava: oznaka obavezna, pocetak pre kraja.
        // Isti Id, pa veze ka godini ostaju ispravne.
        var updated = new AcademicYear(
            existing.Id,
            command.Label,
            command.StartsAt,
            command.EndsAt);

        if (!await _repository.TryUpdateAsync(updated, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Akademska godina '{updated.Label}' već postoji.");
        }
    }
}
