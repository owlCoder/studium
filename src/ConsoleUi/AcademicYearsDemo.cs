using Application.Academic;
using Domain.Academic;
using Infrastructure.Academic;

namespace ConsoleUi;

/// <summary>
/// Demonstracija ST-004 (akademske godine): kreiranje, lista, pretraga, izmena.
/// </summary>
public static class AcademicYearsDemo
{
    private const string DateFormat = "dd.MM.yyyy. HH:mm";

    public static async Task RunAsync()
    {
        var repository = new InMemoryAcademicYearRepository();
        var create = new CreateAcademicYearHandler(repository);
        var list = new ListAcademicYearsHandler(repository);
        var update = new UpdateAcademicYearHandler(repository);

        Console.WriteLine();
        Console.WriteLine("=== ST-004 | Akademske godine ===");

        Console.WriteLine("\n1) Kreiranje dve godine");
        var id1 = await create.HandleAsync(new CreateAcademicYearCommand(
            "2026/2027", new DateTime(2026, 10, 1), new DateTime(2027, 9, 30)));
        var id2 = await create.HandleAsync(new CreateAcademicYearCommand(
            "2027/2028", new DateTime(2027, 10, 1), new DateTime(2028, 9, 30)));
        Console.WriteLine("Kreirane.");

        Console.WriteLine("\n2) Lista");
        PrintYears(await list.HandleAsync());

        Console.WriteLine("\n3) Odbijeni unosi pri kreiranju (podaci ostaju nepromenjeni)");
        await ExpectRejectedAsync("Duplikat oznake (sa razmacima)", () => create.HandleAsync(
            new CreateAcademicYearCommand(" 2026/2027 ", new DateTime(2030, 10, 1), new DateTime(2031, 9, 30))));
        await ExpectRejectedAsync("Pocetak posle kraja", () => create.HandleAsync(
            new CreateAcademicYearCommand("2028/2029", new DateTime(2029, 9, 30), new DateTime(2028, 10, 1))));
        await ExpectRejectedAsync("Prazna oznaka", () => create.HandleAsync(
            new CreateAcademicYearCommand("   ", new DateTime(2029, 10, 1), new DateTime(2030, 9, 30))));

        Console.WriteLine("\n4) Pretraga po oznaci: '2028'");
        PrintYears(await list.HandleAsync("2028"));

        Console.WriteLine("\n5) Izmena godine 2027/2028 (novi krajnji datum)");
        await update.HandleAsync(new UpdateAcademicYearCommand(
            id2, "2027/2028", new DateTime(2027, 10, 1), new DateTime(2028, 10, 15)));
        PrintYears(await list.HandleAsync());

        Console.WriteLine("\n6) Odbijene izmene (stanje ostaje isto)");
        await ExpectRejectedAsync("Oznaka vec zauzeta drugom godinom", () => update.HandleAsync(
            new UpdateAcademicYearCommand(id2, "2026/2027", new DateTime(2027, 10, 1), new DateTime(2028, 9, 30))));
        await ExpectRejectedAsync("Pocetak posle kraja", () => update.HandleAsync(
            new UpdateAcademicYearCommand(id1, "2026/2027", new DateTime(2027, 9, 30), new DateTime(2026, 10, 1))));
        await ExpectRejectedAsync("Nepostojeca godina", () => update.HandleAsync(
            new UpdateAcademicYearCommand(Guid.NewGuid(), "2099/2100", new DateTime(2099, 10, 1), new DateTime(2100, 9, 30))));

        Console.WriteLine("\n7) Stanje posle odbijenih izmena");
        PrintYears(await list.HandleAsync());
    }

    private static void PrintYears(IReadOnlyList<AcademicYear> years)
    {
        if (years.Count == 0)
        {
            Console.WriteLine("  (nema rezultata)");
            return;
        }

        foreach (var y in years)
        {
            Console.WriteLine($"  {y.Label}  {y.StartsAt.ToString(DateFormat)} - {y.EndsAt.ToString(DateFormat)}");
        }
    }

    private static async Task ExpectRejectedAsync(string title, Func<Task> action)
    {
        try
        {
            await action();
            Console.WriteLine($"  [GRESKA DEMOA] {title}: operacija je prosla, a nije smela.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"  ODBIJENO - {title}: {ex.Message}");
        }
    }
}
