using System.Text;
using ConsoleUi.Scenarios;
using Infrastructure.Dashboard;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0 && args[0] == "st-031-032")
    await St031St032Scenario.RunAsync();
else if (args.Length > 0 && args[0] == "st-007")
    PrintSupportedFeatures();
else
    await CatalogDemo.RunAsync();

// US-7.6 (ST-007): registar podržanih funkcija.
// Pokretanje: dotnet run --project src/ConsoleUi -- st-007
static void PrintSupportedFeatures()
{
    var registry = SupportedFeatures.CreateRegistry();

    Console.WriteLine("STUDIUM | ST-007: registar podržanih funkcija");
    Console.WriteLine("Podržane vrste aktivnosti:");
    foreach (var type in registry.SupportedActivityTypes)
        Console.WriteLine($"  {type.Code} ({type.Area})");

    foreach (var check in new[] { "assignment", "quiz" })
        Console.WriteLine($"Da li je '{check}' podržano? {(registry.IsActivitySupported(check) ? "da" : "ne")}");
}
