using Application.Courses;
using Infrastructure.Demo;

namespace ConsoleUi;

/// <summary>
/// Ulazna logika konzolne aplikacije, izdvojena iz Program.cs da bi mogla da se testira
/// bez pokretanja procesa. Podrazumevani start (bez argumenata) ne sme da čita ulaz.
/// </summary>
public static class ConsoleApp
{
    public const string St019Option = "--st-019";

    public const int ExitSuccess = 0;
    public const int ExitUnknownArgument = 1;

    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        if (args.Length == 0)
        {
            await RunCatalogDemoAsync(output);
            return ExitSuccess;
        }

        if (args.Length == 1 && string.Equals(args[0], St019Option, StringComparison.OrdinalIgnoreCase))
        {
            RunSt019GroupSelection(input, output);
            return ExitSuccess;
        }

        output.WriteLine($"Nepoznat argument: {string.Join(' ', args)}");
        output.WriteLine("Upotreba:");
        output.WriteLine("  (bez argumenata)  demonstracija čitanja kataloga, bez interaktivnog unosa");
        output.WriteLine($"  {St019Option}          ST-019: interaktivni izbor grupa za prikaz nastavnog sadržaja");
        return ExitUnknownArgument;
    }

    // Polazni demo: ponašanje mora ostati isto kao na grani main i bez čitanja ulaza.
    private static async Task RunCatalogDemoAsync(TextWriter output)
    {
        output.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

        var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
        var courses = await handler.HandleAsync();

        foreach (var course in courses)
        {
            output.WriteLine($"{course.Code} — {course.Name}");
        }

        output.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
    }

    // ST-019 demo nad sintetičkim (mock) grupama; pokreće se samo uz --st-019.
    private static void RunSt019GroupSelection(TextReader input, TextWriter output)
    {
        output.WriteLine("--- ST-019: Izbor grupa za prikaz nastavnog sadržaja ---");

        var mockGroups = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var selectedGroups = SelectGroupsForContent(mockGroups, input, output);

        if (selectedGroups.Count == 0)
        {
            output.WriteLine("Nije izabrana nijedna važeća grupa; sadržaju nije dodeljena publika.");
            return;
        }

        var service = new SelectContentGroupsService();
        var result = service.AssignGroupsToContent(Guid.NewGuid(), selectedGroups);
        output.WriteLine($"[USPEH] Dodeljeno {result.TargetGroupIds.Count} grupa nastavnom sadržaju.");
    }

    private static List<Guid> SelectGroupsForContent(List<Guid> availableGroupIds, TextReader input, TextWriter output)
    {
        for (int i = 0; i < availableGroupIds.Count; i++)
        {
            output.WriteLine($"[{i + 1}] Grupa ID: {availableGroupIds[i]}");
        }

        output.WriteLine();
        output.Write("Unesite redne brojeve grupa (odvojene zarezom, npr. 1, 2): ");

        // Kraj ulaza (null) tretira se kao prazan izbor, da scenario ne visi u skriptama.
        string line = input.ReadLine() ?? string.Empty;

        return line
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out int val) ? val - 1 : -1)
            .Where(idx => idx >= 0 && idx < availableGroupIds.Count)
            .Select(idx => availableGroupIds[idx])
            .ToList();
    }
}
