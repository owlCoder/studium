using Application.Contributions;

namespace ConsoleUi;

// PRIKAZ: ispis doprinosa. Prima TextWriter da bi moglo da se testira.
// Dokaz je prilog evidenciji, a ne potvrda kvaliteta ili autorstva (pravilo ST-026),
// zato se nigde ne ispisuje "verifikovano" ili slicno.
public static class ContributionPrinter
{
    public static void PrintList(IReadOnlyList<ContributionSummaryDto> items, TextWriter output)
    {
        if (items.Count == 0)
        {
            output.WriteLine("Nema evidentiranih doprinosa.");
            return;
        }

        output.WriteLine($"{"ID",-4} {"Zaduž.",-7} {"Faza",-5} {"Stanje",-18} Poslednja izmena");
        output.WriteLine(new string('-', 68));

        foreach (var c in items)
        {
            output.WriteLine($"{c.Id,-4} {c.IdTask,-7} {c.PhaseId,-5} {c.Status,-18} {c.LastChangedAt:dd.MM.yyyy HH:mm}");
        }
    }

    public static void PrintDetails(ContributionDetailsDto d, TextWriter output)
    {
        output.WriteLine($"Doprinos #{d.Id} | zaduženje: {d.IdTask} | autor: {d.AuthorId} | faza: {d.PhaseId} | stanje: {d.Status}");
        output.WriteLine($"Podnet: {d.CreatedAt:dd.MM.yyyy HH:mm} | poslednja izmena: {d.LastChangedAt:dd.MM.yyyy HH:mm}");
        output.WriteLine();
        output.WriteLine("Trenutna verzija:");
        PrintRevision(d.CurrentRevision, output, "  ");

        if (d.History.Count > 1)
        {
            output.WriteLine();
            output.WriteLine("Istorija verzija:");
            foreach (var r in d.History)
            {
                PrintRevision(r, output, "  ");
                output.WriteLine();
            }
        }
    }

    private static void PrintRevision(ContributionRevisionDto r, TextWriter output, string indent)
    {
        output.WriteLine($"{indent}Verzija {r.RevisionNumber} ({r.CreatedAt:dd.MM.yyyy HH:mm})");
        output.WriteLine($"{indent}Opis: {r.Description}");

        if (!string.IsNullOrWhiteSpace(r.ReviewerNote))
        {
            output.WriteLine($"{indent}Napomena nastavnika: {r.ReviewerNote}");
        }

        if (r.Evidence.Count == 0)
        {
            output.WriteLine($"{indent}Dokazi: nema priloženih dokaza");
            return;
        }

        output.WriteLine($"{indent}Dokazi:");
        foreach (var e in r.Evidence)
        {
            output.WriteLine($"{indent}  - {e.Kind}: {e.Value}");
        }
    }
}
