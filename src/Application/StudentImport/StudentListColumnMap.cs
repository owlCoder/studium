namespace Application.StudentImport;

/// <summary>Повезује колоне заглавља са пољима студента.</summary>
/// <remarks>Називи колона су изричито наведени. Лозинке и токени се игноришу уз упозорење.</remarks>
public sealed class StudentListColumnMap
{
    private static readonly string[] UsernameHeaders = { "username" };

    private static readonly string[] FirstNameHeaders = { "firstname", "ime" };

    private static readonly string[] LastNameHeaders = { "lastname", "prezime" };

    private static readonly string[] FullNameHeaders = { "fullname", "imeprezime" };

    private static readonly string[] EmailHeaders = { "email" };

    private static readonly string[] ForbiddenHeaders =
        { "password", "lozinka", "sifra", "token", "tokenzaaktivaciju", "aktivacionitoken" };

    private StudentListColumnMap(
        int username, int firstName, int lastName, int fullName, int email,
        IReadOnlyList<string> warnings)
    {
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        FullName = fullName;
        Email = email;
        Warnings = warnings;
    }

    public int Username { get; }

    public int FirstName { get; }

    public int LastName { get; }

    public int FullName { get; }

    public int Email { get; }

    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Повезује заглавље са пољима; одбија датотеку без обавезне колоне.</summary>
    public static StudentListColumnMap From(CsvRow header)
    {
        var names = header.Cells.Select(Normalize).ToList();

        var username = IndexOf(names, UsernameHeaders);
        if (username < 0)
        {
            throw new ImportFileRejectedException(
                "Datoteka ne sadrzi kolonu sa jedinstvenim korisnickim imenom.");
        }

        var firstName = IndexOf(names, FirstNameHeaders);
        var lastName = IndexOf(names, LastNameHeaders);
        var fullName = IndexOf(names, FullNameHeaders);

        var warnings = new List<string>();

        // За постојећи налог довољно је корисничко име; нов налог тражи име и презиме.
        if (firstName < 0 && lastName < 0 && fullName < 0)
        {
            warnings.Add(
                "Datoteka ne sadrzi ime i prezime; redovi za postojece naloge su upotrebljivi, "
                + "a novi nalozi ce biti prijavljeni kao greska jer ih ST-001 trazi.");
        }
        else if ((firstName < 0 || lastName < 0) && fullName >= 0)
        {
            warnings.Add(
                "Ime i prezime su dati u jednoj koloni; razdvajanje po prvoj praznini je "
                + "pretpostavka koju treba potvrditi.");
        }
        else if (firstName < 0 || lastName < 0)
        {
            warnings.Add(
                "Datoteka ne sadrzi i ime i prezime; novi nalozi ce biti prijavljeni kao "
                + "greska jer ih ST-001 trazi.");
        }

        foreach (var forbidden in ForbiddenHeaders.Where(names.Contains))
        {
            warnings.Add(
                $"Kolona '{forbidden}' je ignorisana: lozinke i aktivacioni tokeni se ne "
                + "unose kroz CSV.");
        }

        return new StudentListColumnMap(
            username, firstName, lastName, fullName,
            IndexOf(names, EmailHeaders), warnings);
    }

    /// <summary>Вредност ћелије или празан текст када колоне нема.</summary>
    public string ValueOf(IReadOnlyList<string> cells, int index)
        => index >= 0 && index < cells.Count ? cells[index].Trim() : string.Empty;

    /// <summary>Име и презиме из одвојених колона, или раздвојени из једне колоне по првој празнини.</summary>
    public (string FirstName, string LastName) ResolveName(IReadOnlyList<string> cells)
    {
        if (FirstName >= 0 && LastName >= 0)
            return (ValueOf(cells, FirstName), ValueOf(cells, LastName));

        if (FullName < 0)
            return (ValueOf(cells, FirstName), ValueOf(cells, LastName));

        var full = ValueOf(cells, FullName);
        if (string.IsNullOrWhiteSpace(full))
            return (ValueOf(cells, FirstName), ValueOf(cells, LastName));

        var space = full.IndexOf(' ');
        var (fromFullFirst, fromFullLast) = space < 0
            ? (full, string.Empty)
            : (full[..space].Trim(), full[(space + 1)..].Trim());

        return (
            FirstName >= 0 ? ValueOf(cells, FirstName) : fromFullFirst,
            LastName >= 0 ? ValueOf(cells, LastName) : fromFullLast);
    }

    private static int IndexOf(List<string> headers, string[] candidates)
    {
        var matches = headers.Select((name, index) => (name, index))
            .Where(item => candidates.Contains(item.name)).Select(item => item.index).ToArray();
        if (matches.Length > 1)
            throw new ImportFileRejectedException("Vise kolona oznacava isto polje; uklonite dvosmisleno zaglavlje.");
        return matches.Length == 0 ? -1 : matches[0];
    }

    private static string Normalize(string value)
        => value.Trim().Trim('"').ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("č", "c").Replace("ć", "c").Replace("ž", "z")
            .Replace("š", "s").Replace("đ", "d");
}
