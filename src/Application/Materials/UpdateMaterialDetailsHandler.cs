namespace Application.Materials;

/// <summary>
/// Slučaj upotrebe: izmena naziva i opisa postojećeg materijala (ST-009, US-02).
/// Vraća <c>false</c> ako materijal ne postoji; nevažeći naziv izaziva
/// <see cref="ArgumentException"/> bez promene sačuvanog materijala.
/// Uspešna izmena beleži vreme poslednje izmene iz zadatog izvora vremena.
/// Provera da je korisnik nastavnik izvođenja (ST-005) nije deo ovog koraka.
/// </summary>
public sealed class UpdateMaterialDetailsHandler
{
    private readonly IMaterialRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateMaterialDetailsHandler(IMaterialRepository repository, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HandleAsync(
        UpdateMaterialDetailsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _repository.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return false;

        material.UpdateDetails(command.Name, command.Description, _timeProvider.GetUtcNow());
        await _repository.UpdateAsync(material, cancellationToken);

        return true;
    }
}
