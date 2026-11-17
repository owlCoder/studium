using System.Text;

namespace Application.StudentImport;

/// <summary>Чита CSV записе; значење колона одређује <see cref="StudentListColumnMap"/>.</summary>
/// <remarks>Поље под наводницима може садржати раздељивач и прелом реда. Празни редови се прескачу.</remarks>
public sealed class CsvReader
{
    /// <summary>Ограничење величине улаза ради заштите меморије.</summary>
    public const int MaxCharacters = 8 * 1024 * 1024;

    /// <summary>Највећи број непразних CSV записа, укључујући заглавље.</summary>
    public const int MaxRows = 100_000;

    /// <summary>Ограничење дужине поља ради заштите меморије.</summary>
    public const int MaxFieldLength = 10_000;

    /// <summary>Ограничење броја колона ради заштите меморије.</summary>
    public const int MaxColumns = 256;

    /// <summary>Враћа непразне редове са физичким бројем реда у датотеци (бројање од 1).</summary>
    /// <exception cref="ImportFileRejectedException">
    /// Датотека је празна, прелази ограничења или има погрешне наводнике.
    /// </exception>
    public CsvDocument Read(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
            throw new ImportFileRejectedException("Datoteka je prazna.");

        if (csvText.Length > MaxCharacters)
        {
            throw new ImportFileRejectedException(
                "Datoteka je prevelika za uvoz; podelite je na vise datoteka.");
        }

        var text = csvText.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
        var delimiter = DetectDelimiter(text);

        var rows = Parse(text, delimiter);

        if (rows.Count == 0)
            throw new ImportFileRejectedException("Datoteka je prazna.");

        return new CsvDocument(rows, delimiter);
    }

    /// <summary>Препознаје раздељивач у заглављу, ван наводника и после празних редова.</summary>
    private static char DetectDelimiter(string text)
    {
        var commas = 0;
        var semicolons = 0;
        var quoted = false;
        var sawContentOnLine = false;

        foreach (var c in text)
        {
            if (c == '"')
            {
                quoted = !quoted;
                sawContentOnLine = true;
                continue;
            }

            if (c == '\n')
            {
                if (!quoted && sawContentOnLine) break;
                continue;
            }

            if (!char.IsWhiteSpace(c)) sawContentOnLine = true;
            if (quoted) continue;

            if (c == ',') commas++;
            if (c == ';') semicolons++;
        }

        return semicolons > commas ? ';' : ',';
    }

    private static List<CsvRow> Parse(string text, char delimiter)
    {
        var rows = new List<CsvRow>();
        var cells = new List<string>();
        var current = new StringBuilder();

        var line = 1;
        var startLine = 1;
        var quoted = false;
        var closedQuote = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (quoted)
            {
                if (c == '"')
                {
                    // Дупли наводник унутар поља је један наводник; сам је крај поља.
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        AppendChecked(current, '"', line);
                        i++;
                    }
                    else
                    {
                        quoted = false;
                        closedQuote = true;
                    }

                    continue;
                }

                // Прелом реда унутар наводника не започиње нови CSV запис.
                if (c == '\n') line++;

                AppendChecked(current, c, line);
                continue;
            }

            if (closedQuote && c != delimiter && c != '\n')
                throw new ImportFileRejectedException($"Neocekivan znak posle navodnika u redu {line}.");

            switch (c)
            {
                case '"':
                    if (current.Length != 0)
                        throw new ImportFileRejectedException($"Navodnik unutar necitiranog polja u redu {line}.");
                    quoted = true;
                    break;

                case '\n':
                    AddCell(cells, current, line);
                    current.Clear();
                    closedQuote = false;
                    AddIfData(rows, cells, startLine);
                    cells = new List<string>();
                    line++;
                    startLine = line;
                    break;

                default:
                    if (c == delimiter)
                    {
                        AddCell(cells, current, line);
                        current.Clear();
                        closedQuote = false;
                    }
                    else
                    {
                        AppendChecked(current, c, line);
                    }

                    break;
            }
        }

        if (quoted)
        {
            throw new ImportFileRejectedException(
                "Datoteka ima nezatvoren navodnik; proverite poslednje polje.");
        }

        if (current.Length > 0 || cells.Count > 0)
        {
            AddCell(cells, current, line);
            AddIfData(rows, cells, startLine);
        }

        return rows;
    }

    private static void AddCell(List<string> cells, StringBuilder current, int line)
    {
        if (cells.Count >= MaxColumns)
            throw new ImportFileRejectedException($"Red {line} ima previse kolona.");
        cells.Add(current.ToString());
    }

    private static void AppendChecked(StringBuilder current, char c, int line)
    {
        if (current.Length >= MaxFieldLength)
        {
            throw new ImportFileRejectedException(
                $"Polje u redu {line} je predugacko; proverite sadrzaj datoteke.");
        }

        current.Append(c);
    }

    /// <summary>Ред са само празним пољима се прескаче, али физички број реда остаје.</summary>
    private static void AddIfData(List<CsvRow> rows, List<string> cells, int lineNumber)
    {
        if (cells.All(string.IsNullOrWhiteSpace)) return;

        if (rows.Count >= MaxRows)
        {
            throw new ImportFileRejectedException(
                "Datoteka ima previse redova za uvoz; podelite je na vise datoteka.");
        }

        rows.Add(new CsvRow(lineNumber, cells));
    }
}

/// <summary>Један CSV запис са раздвојеним ћелијама и бројем почетног физичког реда.</summary>
public sealed record CsvRow(int Number, IReadOnlyList<string> Cells);

/// <summary>Прочитани CSV записи; први непразан запис је заглавље.</summary>
public sealed record CsvDocument(IReadOnlyList<CsvRow> Rows, char Delimiter)
{
    public CsvRow Header => Rows[0];

    public IEnumerable<CsvRow> DataRows => Rows.Skip(1);
}
