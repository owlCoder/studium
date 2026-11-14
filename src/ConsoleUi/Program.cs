using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Application.Courses;
using Infrastructure.Demo;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
{
    Console.WriteLine($"{course.Code} - {course.Name}");
}

// === ST-019: Izbor grupa za prikaz nastavnog sadržaja ===
Console.WriteLine("\n--- ST-019: Izbor grupa za prikaz nastavnog sadržaja ---");
var mockGroups = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
var selectedGroups = SelectGroupsForContent(mockGroups);

var service = new SelectContentGroupsService();
if (selectedGroups.Any())
{
    var result = service.AssignGroupsToContent(Guid.NewGuid(), selectedGroups);
    Console.WriteLine($"[USPEH] Dodeljeno {result.TargetGroupIds.Count} grupa nastavnom sadržaju.");
}

static List<Guid> SelectGroupsForContent(List<Guid> availableGroupIds)
{
    for (int i = 0; i < availableGroupIds.Count; i++)
    {
        Console.WriteLine($"[{i + 1}] Grupa ID: {availableGroupIds[i]}");
    }

    Console.Write("\nUnesite redne brojeve grupa (odvojene zarezom, npr. 1, 2): ");
    string input = Console.ReadLine() ?? "";

    return input
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => int.TryParse(x.Trim(), out int val) ? val - 1 : -1)
        .Where(idx => idx >= 0 && idx < availableGroupIds.Count)
        .Select(idx => availableGroupIds[idx])
        .ToList();
}