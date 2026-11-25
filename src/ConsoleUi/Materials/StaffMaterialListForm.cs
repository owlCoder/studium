using Application.Materials;
using Domain.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna lista materijala izvođenja za nastavnika (ST-009, US-08, tačka 3):
/// uz svaki materijal prikazuje oznaku stanja (nacrt, objavljen, povučen),
/// a uz nacrt i dugme <see cref="PublishButton"/>. Dugme se „pritiska” unosom
/// rednog broja materijala.
/// Ovo je prezentacija, ne bezbednosna kontrola: pravo na listu proverava
/// <see cref="ListMaterialsForStaffHandler"/>, a pravo i uslove objave
/// (stanje nacrta, sačuvane datoteke priloga) <see cref="PublishMaterialHandler"/>.
/// Posle uspešne objave lista se ponovo ispisuje sa novim stanjem.
/// </summary>
public sealed class StaffMaterialListForm
{
    public const string PublishButton = "[Objavi]";

    private readonly ListMaterialsForStaffHandler _listHandler;
    private readonly PublishMaterialHandler _publishHandler;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public StaffMaterialListForm(
        ListMaterialsForStaffHandler listHandler,
        PublishMaterialHandler publishHandler,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(listHandler);
        ArgumentNullException.ThrowIfNull(publishHandler);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _listHandler = listHandler;
        _publishHandler = publishHandler;
        _input = input;
        _output = output;
    }

    /// <summary>
    /// Ispisuje listu i nudi objavu nacrta. Vraća <c>true</c> samo ako je materijal objavljen.
    /// </summary>
    public async Task<bool> RunAsync(
        Guid requestingUserId,
        Guid courseOfferingId,
        CancellationToken cancellationToken = default)
    {
        var materials = await ShowListAsync(requestingUserId, courseOfferingId, cancellationToken);
        if (materials is null || !materials.Any(m => m.PublicationState == MaterialPublicationState.Draft))
            return false;

        _output.Write($"Redni broj materijala za {PublishButton} (Enter = bez objave): ");
        var choice = _input.ReadLine();
        if (string.IsNullOrWhiteSpace(choice))
            return false;

        if (!int.TryParse(choice.Trim(), out var number) || number < 1 || number > materials.Count)
        {
            _output.WriteLine("Neispravan izbor.");
            return false;
        }

        var selected = materials[number - 1];
        PublishMaterialResult result;
        try
        {
            result = await _publishHandler.HandleAsync(
                new PublishMaterialCommand(requestingUserId, selected.Id),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Objava odbijena: {ex.Message}");
            return false;
        }

        switch (result.Outcome)
        {
            case PublishMaterialOutcome.Published:
                _output.WriteLine($"Materijal „{selected.Name}” je objavljen.");
                await ShowListAsync(requestingUserId, courseOfferingId, cancellationToken);
                return true;

            case PublishMaterialOutcome.MissingAttachmentFiles:
                _output.WriteLine(
                    "Objava odbijena: nedostaju datoteke priloga: " +
                    $"{string.Join(", ", result.MissingFileNames)}. Materijal ostaje nacrt.");
                return false;

            case PublishMaterialOutcome.NotDraft:
                _output.WriteLine("Objava odbijena: objaviti se može samo materijal u stanju nacrta.");
                return false;

            default:
                _output.WriteLine("Materijal nije pronađen.");
                return false;
        }
    }

    /// <summary>
    /// Oznaka stanja objave koja se prikazuje u listi.
    /// </summary>
    public static string StateLabel(MaterialPublicationState state) => state switch
    {
        MaterialPublicationState.Draft => "Nacrt",
        MaterialPublicationState.Published => "Objavljen",
        MaterialPublicationState.Withdrawn => "Povučen",
        _ => "Nepoznato stanje"
    };

    private async Task<IReadOnlyList<StaffMaterialListItem>?> ShowListAsync(
        Guid requestingUserId,
        Guid courseOfferingId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StaffMaterialListItem> materials;
        try
        {
            materials = await _listHandler.HandleAsync(
                new ListMaterialsForStaffQuery(requestingUserId, courseOfferingId),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Lista materijala nije dostupna: {ex.Message}");
            return null;
        }

        _output.WriteLine($"Materijali izvođenja ({materials.Count}):");
        for (var i = 0; i < materials.Count; i++)
        {
            var material = materials[i];
            var button = material.PublicationState == MaterialPublicationState.Draft
                ? $" {PublishButton}"
                : string.Empty;

            _output.WriteLine($"  {i + 1}. {material.Name} — {StateLabel(material.PublicationState)}{button}");
        }

        return materials;
    }
}
