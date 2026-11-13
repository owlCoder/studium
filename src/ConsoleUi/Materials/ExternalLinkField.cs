using Domain.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolno polje za unos spoljne veze materijala (ST-009, US-03).
/// Traži punu adresu (npr. https://example.org/predavanje); prazan unos (Enter)
/// znači da se veza ne dodaje. Neispravna adresa ispisuje razlog i ponavlja pitanje,
/// a kraj ulaza odustaje od unosa.
/// Pravila adrese sprovodi domenski model <see cref="MaterialExternalLink"/>; polje ih ne duplira.
/// NAPOMENA: polje samo prikuplja i proverava vezu, ne čuva je. Čuvanje veze, ubacivanje
/// polja u formu materijala i provera nastavnog osoblja (ST-005) nisu deo ovog koraka.
/// </summary>
public sealed class ExternalLinkField
{
    public const string Prompt = "Spoljna veza (puna adresa, npr. https://example.org; Enter = bez veze): ";

    private readonly TextReader _input;
    private readonly TextWriter _output;

    public ExternalLinkField(TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _input = input;
        _output = output;
    }

    /// <summary>
    /// Vraća proverenu vezu za zadati materijal ili <c>null</c> ako korisnik
    /// ne doda vezu (prazan unos) ili prekine unos.
    /// </summary>
    public MaterialExternalLink? Read(Guid materialId)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("Materijal mora biti zadat.", nameof(materialId));

        while (true)
        {
            _output.Write(Prompt);
            var line = _input.ReadLine();

            if (line is null)
            {
                _output.WriteLine();
                _output.WriteLine("Unos je prekinut. Veza nije dodata.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                _output.WriteLine("Veza nije dodata.");
                return null;
            }

            try
            {
                var link = new MaterialExternalLink(Guid.NewGuid(), materialId, line);
                _output.WriteLine($"Uneta veza: {link.Url.AbsoluteUri}");
                return link;
            }
            catch (ArgumentException ex)
            {
                _output.WriteLine($"Veza nije prihvaćena: {MessageWithoutParameter(ex)}");
            }
        }
    }

    private static string MessageWithoutParameter(ArgumentException ex) =>
        ex.ParamName is null
            ? ex.Message
            : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty);
}
