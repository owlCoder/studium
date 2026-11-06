using Application.Courses;
using Application.Groups;
using Infrastructure;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

builder.Services.AddSingleton<IGroupMembershipRepository, InMemoryGroupMembershipRepository>();
builder.Services.AddScoped<AddStudentToGroupHandler>();
builder.Services.AddScoped<RemoveStudentFromGroupHandler>();

builder.Services.AddAuthentication("TestScheme")
    .AddCookie("TestScheme", options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GroupOwnerPolicy", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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
    return Results.Ok(new { message = "Student je uspesno dodat u grupu." });
}).RequireAuthorization("GroupOwnerPolicy");

app.MapDelete("/groups/{groupId}/students/{studentId}", async (
    Guid groupId,
    Guid studentId,
    [FromBody]RemoveStudentRequest request,
    RemoveStudentFromGroupHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new RemoveStudentFromGroupCommand(groupId, studentId, request.EndDate);
    await handler.Handle(command, cancellationToken);
    return Results.Ok(new { message = "Clanstvo studenta u grupi je uspešno zavrseno." });
}).RequireAuthorization("GroupOwnerPolicy");

app.Run();


public record AddStudentRequest(Guid StudentId, DateTime StartDate);
public record RemoveStudentRequest(DateTime EndDate);

public partial class Program { }
