using Application.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna strana detalja materijala za studenta (ST-009, US-11):
/// naziv, opis, spoljne veze i lista priloga sa izvornim nazivom, tipom i veličinom.
/// Prikaz je samo prezentacija: ko sme da vidi materijal (objavljen materijal,
/// aktivan student izvođenja) određuje <see cref="GetMaterialDetailsHandler"/>.
/// NAPOMENA: preuzimanje priloga (stavka 3) nije deo ovog koraka.
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

        WriteNameAndDescription(result.Details);
        WriteLinks(result.Details.Links);
        WriteAttachments(result.Details.Attachments);
        return result.Details;
    }

    private void WriteNameAndDescription(MaterialDetails details)
    {
        var description = details.Description.Length == 0 ? "(bez opisa)" : details.Description;

        _output.WriteLine(details.Name);
        _output.WriteLine($"Opis: {description}");
        _output.WriteLine();
    }

    private void WriteLinks(IReadOnlyList<MaterialDetailsLink> links)
    {
        _output.WriteLine($"Spoljne veze ({links.Count}):");
        if (links.Count == 0)
            _output.WriteLine("  (nema veza)");

        foreach (var link in links)
            _output.WriteLine($"  - {link.Url}");
    }

    private void WriteAttachments(IReadOnlyList<MaterialDetailsAttachment> attachments)
    {
        _output.WriteLine($"Prilozi ({attachments.Count}):");
        if (attachments.Count == 0)
            _output.WriteLine("  (nema priloga)");

        foreach (var attachment in attachments)
            _output.WriteLine($"  - {DescribeAttachment(attachment)}");
    }

    // US-11, stavka 2: izvorni naziv, tip i veličina, u istom obliku kao lista priloga za nastavnika.
    private static string DescribeAttachment(MaterialDetailsAttachment attachment) =>
        $"{attachment.OriginalFileName} ({attachment.ContentType}, {FileSizeFormatter.Format(attachment.SizeInBytes)})";
}
