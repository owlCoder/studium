using Application.Materials;
using Domain.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna forma za izmenu naziva i opisa materijala (ST-009, US-02).
/// Prikazuje postojeće podatke; prazan unos (Enter) zadržava postojeću vrednost,
/// a <see cref="ClearDescriptionMarker"/> briše opis.
/// Ovo je prezentacija, ne bezbednosna kontrola: provera nastavnika (ST-005) nije uključena.
/// </summary>
public sealed class EditMaterialDetailsForm
{
    public const string ClearDescriptionMarker = "-";

    private readonly IMaterialRepository _repository;
    private readonly UpdateMaterialDetailsHandler _handler;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public EditMaterialDetailsForm(
        IMaterialRepository repository,
        UpdateMaterialDetailsHandler handler,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _repository = repository;
        _handler = handler;
        _input = input;
        _output = output;
    }

    /// <summary>
    /// Vraća <c>true</c> samo ako je izmena sačuvana.
    /// </summary>
    public async Task<bool> RunAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        var material = await _repository.GetByIdAsync(materialId, cancellationToken);
        if (material is null)
        {
            _output.WriteLine("Materijal nije pronađen.");
            return false;
        }

        _output.WriteLine("Izmena materijala");
        PrintDetails(material);

        _output.Write($"Novi naziv [{material.Name}]: ");
        var nameInput = _input.ReadLine();
        var newName = string.IsNullOrWhiteSpace(nameInput) ? material.Name : nameInput.Trim();

        _output.Write($"Novi opis [{DisplayDescription(material.Description)}] " +
                      $"(Enter = zadrži, {ClearDescriptionMarker} = obriši): ");
        var descriptionInput = _input.ReadLine();
        var newDescription = descriptionInput?.Trim() switch
        {
            null or "" => material.Description,
            ClearDescriptionMarker => string.Empty,
            var text => text
        };

        if (newName == material.Name && newDescription == material.Description)
        {
            _output.WriteLine("Nema izmena.");
            return false;
        }

        bool updated;
        try
        {
            updated = await _handler.HandleAsync(
                new UpdateMaterialDetailsCommand(materialId, newName, newDescription),
                cancellationToken);
        }
        catch (ArgumentException ex)
        {
            _output.WriteLine($"Izmena nije sačuvana: {ex.Message}");
            return false;
        }

        if (!updated)
        {
            _output.WriteLine("Materijal nije pronađen.");
            return false;
        }

        var saved = await _repository.GetByIdAsync(materialId, cancellationToken);
        _output.WriteLine("Izmena je sačuvana.");
        PrintDetails(saved!);
        return true;
    }

    private void PrintDetails(Material material)
    {
        _output.WriteLine($"  Naziv: {material.Name}");
        _output.WriteLine($"  Opis: {DisplayDescription(material.Description)}");
        _output.WriteLine($"  Poslednja izmena: {DisplayLastModified(material.LastModifiedAt)}");
    }

    private static string DisplayDescription(string description) =>
        description.Length == 0 ? "(bez opisa)" : description;

    private static string DisplayLastModified(DateTimeOffset? lastModifiedAt) =>
        lastModifiedAt is null
            ? "nije menjan"
            : lastModifiedAt.Value.ToString("dd.MM.yyyy. HH:mm:ss 'UTC'");
}
