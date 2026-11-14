using System.Text;
using Application.Courses;
using Infrastructure.Dashboard;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
    Console.WriteLine($"{course.Code} — {course.Name}");

// US-7.6: registar podržanih funkcija
var registry = SupportedFeatures.CreateRegistry();
Console.WriteLine();
Console.WriteLine("Podržane vrste aktivnosti:");
foreach (var type in registry.SupportedActivityTypes)
    Console.WriteLine($"  {type.Code} ({type.Area})");

foreach (var check in new[] { "assignment", "quiz" })
    Console.WriteLine($"Da li je '{check}' podržano? {(registry.IsActivitySupported(check) ? "da" : "ne")}");

Console.WriteLine();

Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
