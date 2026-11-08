using Application.Academic;
using Infrastructure.Academic;
using Infrastructure.Demo;

namespace ConsoleUi;

/// <summary>
/// Demo scenario za ST-004 (KT1): otvaranje akademske godine i izvođenja predmeta.
/// Koristi handlere i repozitorijume u memoriji koje je napisao tim 5.
/// </summary>
public static class St004Demo
{
    public static async Task RunAsync()
    {
        Console.WriteLine();
        Console.WriteLine("--- Demo ST-004: akademske godine i izvođenja ---");

        var courseReader = DemoData.CreateCourseReader();
        var yearRepository = new InMemoryAcademicYearRepository();
        var offeringRepository = new InMemoryCourseOfferingRepository();

        var createYear = new CreateAcademicYearHandler(yearRepository);
        var createOffering = new CreateCourseOfferingHandler(
            offeringRepository, yearRepository, courseReader);

        var course = (await courseReader.ListAsync()).First(c => c.IsActive);
        var yearId = Guid.Empty;

        await Step("1. Otvori akademsku godinu 2026/2027", async () =>
        {
            yearId = await createYear.HandleAsync(new CreateAcademicYearCommand(
                "2026/2027", new DateTime(2026, 10, 1), new DateTime(2027, 9, 30)));
            Console.WriteLine($"   Godina otvorena (id {yearId}).");
        });

        await Step("2. Pokušaj otvoriti istu godinu ponovo", async () =>
        {
            await createYear.HandleAsync(new CreateAcademicYearCommand(
                "2026/2027", new DateTime(2026, 10, 1), new DateTime(2027, 9, 30)));
            Console.WriteLine("   Godina otvorena.");
        });

        await Step("3. Pokušaj godinu sa krajem prije početka", async () =>
        {
            await createYear.HandleAsync(new CreateAcademicYearCommand(
                "2030/2031", new DateTime(2031, 9, 30), new DateTime(2030, 10, 1)));
            Console.WriteLine("   Godina otvorena.");
        });

        await Step($"4. Otvori izvođenje predmeta {course.Code} u godini 2026/2027", async () =>
        {
            var offeringId = await createOffering.HandleAsync(
                new CreateCourseOfferingCommand(course.Id, yearId));
            Console.WriteLine($"   Izvođenje otvoreno (id {offeringId}), stanje: U pripremi.");
        });

        await Step("5. Pokušaj otvoriti isto izvođenje ponovo", async () =>
        {
            await createOffering.HandleAsync(
                new CreateCourseOfferingCommand(course.Id, yearId));
            Console.WriteLine("   Izvođenje otvoreno.");
        });
    }

    private static async Task Step(string title, Func<Task> action)
    {
        Console.WriteLine(title);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ODBIJENO: {ex.Message}");
        }
    }
}
