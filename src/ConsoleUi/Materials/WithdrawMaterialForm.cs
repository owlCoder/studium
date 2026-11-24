using Application.Materials;
using Domain.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna akcija „Povuci“ sa potvrdom (ST-009, US-09, tačka 3).
/// Prikazuje naziv i stanje materijala i traži izričitu potvrdu; podrazumevani odgovor je „ne“,
/// pa prazan unos, kraj ulaza ili bilo šta osim „d“/„da“ otkazuje povlačenje bez promene stanja.
/// Ovo je prezentacija, ne bezbednosna kontrola: pravo povlačenja (nastavno osoblje izvođenja,
/// ST-005) i dozvoljeno stanje proverava <see cref="WithdrawMaterialHandler"/>.
/// </summary>
public sealed class WithdrawMaterialForm
{
    private readonly IMaterialRepository _repository;
    private readonly WithdrawMaterialHandler _handler;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public WithdrawMaterialForm(
        IMaterialRepository repository,
        WithdrawMaterialHandler handler,
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
    /// Vraća <c>true</c> samo ako je materijal povučen.
    /// </summary>
    public async Task<bool> RunAsync(
        Guid requestingUserId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        var material = await _repository.GetByIdAsync(materialId, cancellationToken);
        if (material is null)
        {
            _output.WriteLine("Materijal nije pronađen.");
            return false;
        }

        _output.WriteLine("Povlačenje materijala");
        _output.WriteLine($"  Naziv: {material.Name}");
        _output.WriteLine($"  Stanje: {DisplayState(material.PublicationState)}");

        if (material.PublicationState != MaterialPublicationState.Published)
        {
            _output.WriteLine("Povući se može samo objavljen materijal.");
            return false;
        }

        _output.WriteLine("Posle povlačenja studenti više ne vide materijal. " +
                          "Materijal se ne briše i može se ponovo objaviti.");
        _output.Write($"Povući materijal '{material.Name}'? (d/N): ");
        if (!IsConfirmed(_input.ReadLine()))
        {
            _output.WriteLine("Povlačenje je otkazano. Materijal je i dalje objavljen.");
            return false;
        }

        WithdrawMaterialOutcome outcome;
        try
        {
            outcome = await _handler.HandleAsync(
                new WithdrawMaterialCommand(requestingUserId, materialId),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Materijal nije povučen: {ex.Message}");
            return false;
        }

        switch (outcome)
        {
            case WithdrawMaterialOutcome.Withdrawn:
                _output.WriteLine("Materijal je povučen. Studenti ga više ne vide.");
                return true;
            case WithdrawMaterialOutcome.NotPublished:
                _output.WriteLine("Materijal nije povučen: povući se može samo objavljen materijal.");
                return false;
            default:
                _output.WriteLine("Materijal nije pronađen.");
                return false;
        }
    }

    private static bool IsConfirmed(string? answer) =>
        answer?.Trim().ToLowerInvariant() is "d" or "da";

    private static string DisplayState(MaterialPublicationState state) => state switch
    {
        MaterialPublicationState.Draft => "nacrt",
        MaterialPublicationState.Published => "objavljen",
        MaterialPublicationState.Withdrawn => "povučen",
        _ => state.ToString()
    };
}
