using Domain.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolni prikaz liste spoljnih veza materijala (ST-009, US-03).
/// Prikazuje samo veze zadatog materijala, redosledom kojim su prosleđene,
/// u istom obliku kao lista priloga (<see cref="UploadAttachmentsForm"/>).
/// NAPOMENA: prikaz ne učitava veze sam, jer čuvanje veza još ne postoji;
/// pozivalac prosleđuje veze. Provera ko sme da vidi veze (nastavno osoblje,
/// objavljen materijal za studente) nije deo ovog koraka.
/// </summary>
public sealed class ExternalLinkListView
{
    private readonly TextWriter _output;

    public ExternalLinkListView(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        _output = output;
    }

    /// <summary>
    /// Ispisuje veze materijala i vraća broj prikazanih veza.
    /// Veze koje pripadaju drugom materijalu se ne prikazuju.
    /// </summary>
    public int Show(Guid materialId, IEnumerable<MaterialExternalLink> links)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("Materijal mora biti zadat.", nameof(materialId));
        ArgumentNullException.ThrowIfNull(links);

        var materialLinks = links
            .Where(link => link is not null && link.MaterialId == materialId)
            .ToArray();

        _output.WriteLine($"Spoljne veze materijala ({materialLinks.Length}):");

        if (materialLinks.Length == 0)
        {
            _output.WriteLine("  (nema spoljnih veza)");
            return 0;
        }

        foreach (var link in materialLinks)
            _output.WriteLine($"  - {link.Url.AbsoluteUri}");

        return materialLinks.Length;
    }
}
