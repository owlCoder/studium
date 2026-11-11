using Application.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna forma za unos naziva i opisa novog materijala (ST-009).
/// Naziv je obavezan: prazan unos ponavlja pitanje, a kraj ulaza odustaje
/// bez upisa. Opis nije obavezan (Enter = bez opisa).
/// Forma je samo prezentacija: pravo pristupa (ST-005) i validaciju
/// i dalje sprovodi <see cref="CreateMaterialHandler"/>.
/// </summary>
public sealed class CreateMaterialForm
{
    private readonly CreateMaterialHandler _handler;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public CreateMaterialForm(
        CreateMaterialHandler handler,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _handler = handler;
        _input = input;
        _output = output;
    }

    /// <summary>
    /// Vraća identifikator kreiranog materijala ili <c>null</c> ako ništa nije sačuvano.
    /// </summary>
    public async Task<Guid?> RunAsync(
        Guid requestingUserId,
        Guid courseOfferingId,
        CancellationToken cancellationToken = default)
    {
        _output.WriteLine("Novi materijal (čuva se kao nacrt)");

        string? name;
        while (true)
        {
            _output.Write("Naziv (obavezno): ");
            name = _input.ReadLine();

            if (name is null)
            {
                _output.WriteLine();
                _output.WriteLine("Unos je prekinut. Materijal nije kreiran.");
                return null;
            }

            if (!string.IsNullOrWhiteSpace(name))
                break;

            _output.WriteLine("Naziv materijala je obavezan.");
        }

        _output.Write("Opis (Enter = bez opisa): ");
        var description = _input.ReadLine();

        Guid id;
        try
        {
            id = await _handler.HandleAsync(
                new CreateMaterialCommand(requestingUserId, courseOfferingId, name, description),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Materijal nije kreiran: {ex.Message}");
            return null;
        }
        catch (ArgumentException ex)
        {
            _output.WriteLine($"Materijal nije kreiran: {ex.Message}");
            return null;
        }

        var trimmedDescription = description?.Trim() ?? string.Empty;
        _output.WriteLine("Materijal je sačuvan kao nacrt.");
        _output.WriteLine($"  Naziv: {name.Trim()}");
        _output.WriteLine($"  Opis: {(trimmedDescription.Length == 0 ? "(bez opisa)" : trimmedDescription)}");
        _output.WriteLine("  Stanje: nacrt");
        return id;
    }
}
