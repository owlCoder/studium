using System.Text;
using ConsoleUi.Scenarios;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0 && args[0] == "st-031-032")
    await St031St032Scenario.RunAsync();
else if (args.Length > 0 && args[0] == "st-004")
    await ConsoleUi.AcademicYearsDemo.RunAsync();
else
    await CatalogDemo.RunAsync();