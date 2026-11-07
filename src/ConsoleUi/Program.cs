using System.Text;
using Application.Courses;
using Application.Identity;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
    Console.WriteLine($"{course.Code} — {course.Name}");

ICurrentUser currentUser = FakeCurrentUser.Administrator();
Console.WriteLine($"Prijavljen: {currentUser.Username} ({currentUser.Role})");
Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
