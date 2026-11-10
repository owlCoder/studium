using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Kreira materijal u stanju nacrta (ST-009).
/// Neispravan ulaz (npr. prazan naziv) baca <see cref="ArgumentException"/>
/// pre bilo kakve izmene skladišta.
/// NAPOMENA: provera pripadnosti nastavnom osoblju izvođenja (ST-005)
/// nije deo ovog koraka i dodaje se posebnim zadatkom.
/// </summary>
public sealed class CreateMaterialHandler
{
    private readonly IMaterialRepository _repository;

    public CreateMaterialHandler(IMaterialRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> HandleAsync(
        CreateMaterialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = new Material(
            Guid.NewGuid(),
            command.CourseOfferingId,
            command.Name,
            command.Description);

        await _repository.AddAsync(material, cancellationToken);

        return material.Id;
    }
}
