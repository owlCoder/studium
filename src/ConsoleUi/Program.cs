using System.Text;
using Application.Courses;
using ConsoleUi;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
    Console.WriteLine($"{course.Code} — {course.Name}");

await St004Demo.RunAsync();

Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
await ConsoleUi.AcademicYearsDemo.RunAsync();
