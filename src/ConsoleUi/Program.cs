using System.Text;
using Application.Contributions;
using Application.Courses;
using ConsoleUi;
using Infrastructure.Contributions;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
    Console.WriteLine($"{course.Code} — {course.Name}");

Console.WriteLine();
Console.WriteLine("ST-026 | Pregled doprinosa (student 10)");

var queries = new ContributionQueries(DemoContributions.Create());
var viewer = new Viewer(10, ViewerRole.Student);

ContributionPrinter.PrintList(queries.List(viewer), Console.Out);

Console.WriteLine();
if (queries.TryGetDetails(viewer, 3, out var details, out var error))
    ContributionPrinter.PrintDetails(details!, Console.Out);
else
    Console.WriteLine(error);

Console.WriteLine();
Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
