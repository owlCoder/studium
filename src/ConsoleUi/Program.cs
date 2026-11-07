using System.Text;
using Application.Courses;
using Infrastructure;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
    Console.WriteLine($"{course.Code} — {course.Name}");

Console.WriteLine();
Console.WriteLine("STUDIUM | Demonstracija brojača stanja predaja (ST-013, mock podaci)");

var overview = new MockSubmissionOverviewService();
var counts = await overview.GetStatusCountsForTaskAsync(overview.DefaultTaskId, overview.DefaultMemberId);
var submissions = await overview.GetSubmissionsForTaskAsync(overview.DefaultTaskId, overview.DefaultMemberId);

Console.WriteLine($"Bez predaje:   {counts.WithoutSubmissionCount}");
Console.WriteLine($"Nepregledano:  {counts.UnreviewedCount}");
Console.WriteLine($"Pregledano:    {counts.ReviewedCount}");
Console.WriteLine("Važeće predaje:");
foreach (var s in submissions)
    Console.WriteLine($"  {s.MemberName} - {s.Status} - {s.ContentUrl}");

Console.WriteLine();
Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
