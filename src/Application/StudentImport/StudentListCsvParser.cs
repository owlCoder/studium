namespace Application.StudentImport;

/// <summary>Уговор за претварање текста листе студената у редове увоза.</summary>
public interface IStudentListParser
{
    ParsedStudentList Parse(string csvText);
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
