
using Application;
using Application.Courses;
using Application.Groups;
using Infrastructure;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

builder.Services.AddSingleton<IGroupMembershipRepository, InMemoryGroupMembershipRepository>();
builder.Services.AddSingleton<IGroupLookup, DemoGroupLookup>();
builder.Services.AddSingleton<IStudentLookup, DemoStudentLookup>();

builder.Services.AddScoped<AddStudentToGroupHandler>();
builder.Services.AddScoped<RemoveStudentFromGroupHandler>();
builder.Services.AddScoped<ListStudentGroupsHandler>();

builder.Services.AddSingleton<IGroupRepository>(_ => new InMemoryGroupRepository(DemoData.CreateGroups()));

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

bool IsUserOwnerOfGroup(
    System.Security.Claims.ClaimsPrincipal user,
    Guid groupId)
{
    if (!user.Identity?.IsAuthenticated ?? true)
        return false;

    if (user.IsInRole("Admin"))
        return true;

    var allowedGroupClaim = user.FindFirst("AllowedGroup");

    return allowedGroupClaim != null &&
           Guid.TryParse(allowedGroupClaim.Value, out var allowedId) &&
           allowedId == groupId;
}

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
    MinimalApiAddStudentRequest request,
    AddStudentToGroupHandler handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    if (!IsUserOwnerOfGroup(httpContext.User, groupId))
    {
        return Results.Forbid();
    }

    try
    {
        var command = new AddStudentToGroupCommand(
            groupId, request.StudentId, request.StartDate);

        await handler.Handle(command, ct);

        return Results.Ok(new { message = "Student je uspešno dodat u grupu." });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ex.Message);
    }
}).RequireAuthorization("GroupOwnerPolicy");

app.MapDelete("/groups/{groupId}/students/{studentId}", async (
    Guid groupId,
    Guid studentId,
    [FromBody] MinimalApiRemoveStudentRequest request,
    RemoveStudentFromGroupHandler handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    if (!IsUserOwnerOfGroup(httpContext.User, groupId))
    {
        return Results.Forbid();
    }

    try
    {
        var command = new RemoveStudentFromGroupCommand(
            groupId, studentId, request.EndDate);

        await handler.Handle(command, ct);

        return Results.Ok(new { message = "Članstvo studenta u grupi je uspešno završeno." });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ex.Message);
    }
}).RequireAuthorization("GroupOwnerPolicy");

app.Run();

public class DemoGroupLookup : IGroupLookup
{
    public Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken)
        => Task.FromResult(true);
    public Task<Domain.Entities.Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken) 
    => Task.FromResult<Domain.Entities.Group?>(null);
}

public class DemoStudentLookup : IStudentLookup
{
    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

public record MinimalApiAddStudentRequest(Guid StudentId, DateTime StartDate);
public record MinimalApiRemoveStudentRequest(DateTime EndDate);

public partial class Program { }

