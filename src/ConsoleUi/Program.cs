using System.Text;
using ConsoleUi.Scenarios;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0 && args[0] == "st-012")
{
    using var client = new HttpClient
    {
        BaseAddress = new Uri("http://127.0.0.1:5098"),
        Timeout = TimeSpan.FromSeconds(10)
    };
    await ViewOwnSubmissionScenario.RunAsync(client, Console.In, Console.Out);
    return;
}

if (args.Length > 0 && args[0] == "st-031-032")
    await St031St032Scenario.RunAsync();
else
    await CatalogDemo.RunAsync();


