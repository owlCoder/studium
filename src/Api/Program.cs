using Application.Courses;
using Application.Groups;
using Infrastructure;
using Infrastructure.Demo;
using Application.TeachingGroups;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

builder.Services.AddSingleton<IGroupMembershipRepository, InMemoryGroupMembershipRepository>();
builder.Services.AddScoped<AddStudentToGroupHandler>();
builder.Services.AddScoped<RemoveStudentFromGroupHandler>();

builder.Services.AddSingleton<ITeachingGroupRepository, InMemoryTeachingGroupRepository>();
builder.Services.AddScoped<CreateTeachingGroupHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Javno dostupan demonstracioni endpoint, bez autentifikacije:
// ne predstavlja funkcionalnost ST-003 i ne koristiti ga za stvarne podatke.
app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.MapGet("/courses", async (
    string? query,
    SearchCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(courses);
});

app.MapPost("/groups/{groupId}/students", async (
    Guid groupId,
    AddStudentRequest request,
    AddStudentToGroupHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new AddStudentToGroupCommand(groupId, request.StudentId, request.StartDate);
    await handler.Handle(command, cancellationToken);
    return Results.Ok(new { message = "Student je uspešno dodat u grupu." });
});

app.MapPost("/teaching-groups", async (
    CreateTeachingGroupRequest request,
    CreateTeachingGroupHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/teaching-groups/{created.Id}", created);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/groups/{groupId}/students/{studentId}", async (
    Guid groupId,
    Guid studentId,
    [FromQuery] DateTime? endDate,
    RemoveStudentFromGroupHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new RemoveStudentFromGroupCommand(groupId, studentId, endDate ?? DateTime.UtcNow);
    await handler.Handle(command, cancellationToken);
    return Results.Ok(new { message = "Članstvo studenta u grupi je uspešno završeno." });
});

app.Run();

public record AddStudentRequest(Guid StudentId, DateTime StartDate);

public partial class Program { }