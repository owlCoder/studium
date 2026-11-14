using Application.Courses;
using Application.Groups;
using Infrastructure;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljasnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
builder.Services.AddScoped<Application.Courses.GetSubmissionHandler>();

builder.Services.AddSingleton<IGroupMembershipRepository, InMemoryGroupMembershipRepository>();
if (DemoAdapterPolicy.AllowDemoAdapters(builder.Environment))
{
    builder.Services.AddSingleton<IGroupLookup, DemoGroupLookup>();
    builder.Services.AddSingleton<IStudentLookup, DemoStudentLookup>();
}
else
{
    builder.Services.AddSingleton<IGroupLookup, UnavailableGroupLookup>();
    builder.Services.AddSingleton<IStudentLookup, UnavailableStudentLookup>();
}

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

bool IsUserOwnerOfGroup(System.Security.Claims.ClaimsPrincipal user, Guid groupId)
{
    if (!user.Identity?.IsAuthenticated ?? true) return false;

    var allowedGroupClaim = user.FindFirst("AllowedGroup");
    if (allowedGroupClaim != null && Guid.TryParse(allowedGroupClaim.Value, out var allowedId))
    {
        return allowedId == groupId;
    }

    return user.IsInRole("Admin") || user.HasClaim("Permission", "ManageGroups");
}

app.MapPost("/groups/{groupId}/students", async (
    Guid groupId,
    [FromBody] AddStudentRequest request,
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
        var command = new AddStudentToGroupCommand(groupId, request.StudentId, request.StartDate);
        await handler.Handle(command, ct);
        return Results.Ok(new { message = "Student je uspešno dodan u grupu." });
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
    [FromBody] RemoveStudentRequest request,
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
        var command = new RemoveStudentFromGroupCommand(groupId, studentId, request.EndDate);
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

// ST-020: Endpoint za dohvat predaje uz autentifikaciju i autorizaciju
app.MapGet("/submissions/{submissionId}", async (
    string submissionId,
    Application.Courses.GetSubmissionHandler handler,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var studentId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(studentId))
    {
        return Results.Unauthorized();
    }

    var result = await handler.HandleAsync(studentId, submissionId, cancellationToken);

    if (result.IsNotFound)
    {
        return Results.NotFound(new { message = "Tražena predaja ne postoji." });
    }

    if (result.IsForbidden || result.Submission is null)
    {
        return Results.Forbid();
    }

    return Results.Ok(result.Submission);
}).RequireAuthorization();

app.Run();

public class DemoGroupLookup : IGroupLookup
{
    private static readonly HashSet<Guid> KnownDemoGroups =
    [
        Guid.Parse("11111111-1111-1111-1111-111111111111")
    ];

    public Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken)
        => Task.FromResult(KnownDemoGroups.Contains(groupId));
}

public class DemoStudentLookup : IStudentLookup
{
    private static readonly HashSet<Guid> KnownActiveStudents =
    [
        Guid.Parse("22222222-2222-2222-2222-222222222222")
    ];

    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken)
        => Task.FromResult(KnownActiveStudents.Contains(studentId));
}

public class UnavailableGroupLookup : IGroupLookup
{
    public Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken)
        => Task.FromResult(false);
}

public class UnavailableStudentLookup : IStudentLookup
{
    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken)
        => Task.FromResult(false);
}

public record AddStudentRequest(Guid StudentId, DateTime StartDate);
public record RemoveStudentRequest(DateTime EndDate);

public partial class Program { }

public static class DemoAdapterPolicy { public static bool AllowDemoAdapters(Microsoft.Extensions.Hosting.IHostEnvironment env) => Microsoft.Extensions.Hosting.HostEnvironmentEnvExtensions.IsDevelopment(env) || Microsoft.Extensions.Hosting.HostEnvironmentEnvExtensions.IsEnvironment(env, "Testing"); }
