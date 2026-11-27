using System.Text;
using Application.Courses;
using ConsoleUi;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;

// Svaki demo se bira argumentom, pa je pokretanje uvek isto i predvidivo.
// Primer: dotnet run --project src/ConsoleUi -- --demo-st004
var runSt004 = args.Contains("--demo-st004");
var runYears = args.Contains("--demo-godine");
var runAll = args.Contains("--sve");

try
{
    if (runSt004 || runAll)
    {
        await St004Demo.RunAsync();
    }

    if (runYears || runAll)
    {
        await AcademicYearsDemo.RunAsync();
    }

    if (!runSt004 && !runYears && !runAll)
    {
        Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

        var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
        foreach (var course in await handler.HandleAsync())
        {
            Console.WriteLine($"{course.Code} — {course.Name}");
        }

        Console.WriteLine();
        Console.WriteLine("Dostupni demo scenariji (tim 5, ST-004):");
        Console.WriteLine("  --demo-st004   akademska godina, izvođenje i prelazi stanja");
        Console.WriteLine("  --demo-godine  kreiranje, pretraga i izmena akademskih godina");
        Console.WriteLine("  --sve          oba scenarija redom");
    }

    return 0;
}
catch (Exception ex)
{
    // Neočekivana greška se prijavljuje jasno, a proces vraća kod različit od 0.
    Console.Error.WriteLine($"Neočekivana greška: {ex.Message}");
    return 1;
}
