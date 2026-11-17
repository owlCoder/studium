namespace Application.StudentImport;

/// <summary>Уговор за претварање текста листе студената у редове увоза.</summary>
public interface IStudentListParser
{
    ParsedStudentList Parse(string csvText);
}

/// <summary>
/// Спаја <see cref="CsvReader"/> и <see cref="StudentListColumnMap"/> у редове за класификацију.
/// </summary>
/// <exception cref="ImportFileRejectedException">
/// Датотека има неисправан формат или недостаје обавезна колона.
/// </exception>
public sealed class StudentListCsvParser : IStudentListParser
{
    private readonly CsvReader _reader;

    public StudentListCsvParser() : this(new CsvReader()) { }

    public StudentListCsvParser(CsvReader reader) => _reader = reader;

    public ParsedStudentList Parse(string csvText)
    {
        var document = _reader.Read(csvText);
        var columns = StudentListColumnMap.From(document.Header);

        var rows = new List<ParsedStudentRow>();
        foreach (var row in document.DataRows)
        {
            var (firstName, lastName) = columns.ResolveName(row.Cells);

            rows.Add(new ParsedStudentRow(
                RowNumber: row.Number,

                // NFC уједначава запис корисничког имена, без промене величине слова.
                Username: UsernameComparison.Canonical(columns.ValueOf(row.Cells, columns.Username)),
                FirstName: firstName,
                LastName: lastName,
                Email: columns.ValueOf(row.Cells, columns.Email)));
        }

        return new ParsedStudentList(rows, columns.Warnings);
    }
}

public sealed record ParsedStudentRow(
    int RowNumber,
    string Username,
    string FirstName,
    string LastName,
    string Email);

public sealed record ParsedStudentList(
    IReadOnlyList<ParsedStudentRow> Rows,
    IReadOnlyList<string> Warnings);

/// <summary>Одбијена улазна датотека; операција не мења податке.</summary>
public sealed class ImportFileRejectedException : Exception
{
    public ImportFileRejectedException(string message) : base(message) { }
}
