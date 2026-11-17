using Application.Courses;
using Infrastructure.Demo;

namespace ConsoleUi.Scenarios;

public static class CatalogDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

        var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
        var courses = await handler.HandleAsync();

        foreach (var course in courses)
            Console.WriteLine($"{course.Code} — {course.Name}");

        Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
    }
}
