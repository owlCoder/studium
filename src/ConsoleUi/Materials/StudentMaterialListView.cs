using Application.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna stranica sa listom materijala predmeta za studenta (ST-009, US-10, tačke 2 i 3):
/// za svaki objavljen materijal prikazuje naziv i kratak opis, a kada materijala nema,
/// prikazuje prazno stanje (<see cref="EmptyStateMessage"/>).
/// Prikaz je samo prezentacija: koje materijale student sme da vidi (objavljeni materijali
/// izvođenja na koje je upisan) određuje <see cref="ListPublishedMaterialsForStudentHandler"/>.
/// Kratak opis: opis u jednom redu, skraćen na <see cref="ShortDescriptionMaxLength"/> znakova
/// sa „…” na kraju; materijal bez opisa prikazuje samo naziv.
/// Prazno stanje (tačka 3) je isto bez obzira na to da li izvođenje nema nijedan materijal
/// ili ima samo nacrte i povučene materijale, pa student ne saznaje da neobjavljeni postoje.
/// Korisnik bez prava ne dobija prazno stanje, nego poruku da lista nije dostupna.
/// </summary>
public sealed class StudentMaterialListView
{
    /// <summary>
    /// Najveći broj znakova opisa koji se prikazuje u listi (dogovor za tačku 2;
    /// specifikacija ne određuje dužinu kratkog opisa).
    /// </summary>
    public const int ShortDescriptionMaxLength = 100;

    /// <summary>
    /// Poruka praznog stanja (tačka 3), u istom obliku kao „(nema veza)” i „(nema priloga)” u ST-009.
    /// </summary>
    public const string EmptyStateMessage = "  (nema objavljenih materijala za ovaj predmet)";

    private const string Ellipsis = "…";

    private readonly ListPublishedMaterialsForStudentHandler _handler;
    private readonly TextWriter _output;

    public StudentMaterialListView(ListPublishedMaterialsForStudentHandler handler, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(output);

        _handler = handler;
        _output = output;
    }

    /// <summary>
    /// Ispisuje listu materijala izvođenja i vraća prikazane stavke,
    /// ili vraća <c>null</c> ako korisnik nema pravo da vidi listu.
    /// </summary>
    public async Task<IReadOnlyList<StudentMaterialListItem>?> ShowAsync(
        Guid requestingUserId,
        Guid courseOfferingId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StudentMaterialListItem> materials;
        try
        {
            materials = await _handler.HandleAsync(
                new ListPublishedMaterialsForStudentQuery(requestingUserId, courseOfferingId),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Lista materijala nije dostupna: {ex.Message}");
            return null;
        }

        _output.WriteLine($"Materijali predmeta ({materials.Count}):");

        if (materials.Count == 0)
        {
            _output.WriteLine(EmptyStateMessage);
            return materials;
        }

        foreach (var material in materials)
        {
            _output.WriteLine($"  - {material.Name}");

            var shortDescription = ShortenDescription(material.Description);
            if (shortDescription.Length > 0)
                _output.WriteLine($"    {shortDescription}");
        }

        return materials;
    }

    /// <summary>
    /// Pravi kratak opis: prelasci u novi red i višestruki razmaci postaju jedan razmak,
    /// a opis duži od <see cref="ShortDescriptionMaxLength"/> znakova se skraćuje i dobija „…”.
    /// Prazan opis ostaje prazan.
    /// </summary>
    public static string ShortenDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var singleLine = string.Join(
            ' ',
            description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (singleLine.Length <= ShortDescriptionMaxLength)
            return singleLine;

        var cut = ShortDescriptionMaxLength;
        // Ne seče se znak koji zauzima dva mesta (npr. emoji) na pola.
        if (char.IsHighSurrogate(singleLine[cut - 1]))
            cut--;

        return singleLine[..cut].TrimEnd() + Ellipsis;
    }
}
