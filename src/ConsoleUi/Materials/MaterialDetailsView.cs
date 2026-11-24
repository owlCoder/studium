using Application.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna strana detalja materijala za studenta (ST-009, US-11, stavka 1):
/// naziv, opis, spoljne veze i lista priloga.
/// Prikaz je samo prezentacija: ko sme da vidi materijal (objavljen materijal,
/// aktivan student izvođenja) određuje <see cref="GetMaterialDetailsHandler"/>.
/// NAPOMENA: tip i veličina priloga (stavka 2) i preuzimanje (stavka 3) nisu deo ovog koraka.
/// </summary>
public sealed class MaterialDetailsView
{
    private readonly GetMaterialDetailsHandler _handler;
    private readonly TextWriter _output;

    public MaterialDetailsView(GetMaterialDetailsHandler handler, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(output);

        _handler = handler;
        _output = output;
    }

    /// <summary>
    /// Ispisuje detalje materijala i vraća ih, ili vraća <c>null</c> ako materijal nije dostupan.
    /// </summary>
    public async Task<MaterialDetails?> ShowAsync(
        Guid requestingUserId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        GetMaterialDetailsResult result;
        try
        {
            result = await _handler.HandleAsync(
                new GetMaterialDetailsQuery(requestingUserId, materialId),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Materijal nije dostupan: {ex.Message}");
            return null;
        }

        if (result.Outcome != GetMaterialDetailsOutcome.Found || result.Details is null)
        {
            _output.WriteLine("Materijal nije dostupan: ne postoji ili nije objavljen.");
            return null;
        }

        var details = result.Details;
        _output.WriteLine(details.Name);
        _output.WriteLine($"Opis: {(details.Description.Length == 0 ? "(bez opisa)" : details.Description)}");
        _output.WriteLine();

        _output.WriteLine($"Spoljne veze ({details.Links.Count}):");
        if (details.Links.Count == 0)
            _output.WriteLine("  (nema veza)");
        foreach (var link in details.Links)
            _output.WriteLine($"  - {link.Url}");

        _output.WriteLine($"Prilozi ({details.Attachments.Count}):");
        if (details.Attachments.Count == 0)
            _output.WriteLine("  (nema priloga)");
        foreach (var attachment in details.Attachments)
            _output.WriteLine($"  - {attachment.OriginalFileName}");

        return details;
    }
}
