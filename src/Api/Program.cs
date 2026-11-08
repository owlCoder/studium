using Application;
using Application.Courses;
using Application.Groups;
using Application.ContentAudience;
using Infrastructure;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

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

builder.Services.AddScoped<Application.Content.IContentReader, Infrastructure.Content.DummyContentReader>();
builder.Services.AddScoped<Application.Groups.IStudentGroupReader, Infrastructure.Groups.DummyStudentGroupReader>();
builder.Services.AddScoped<Application.Content.GetStudentContentHandler>();

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

/* ---------- ST-020: publika nastavnog sadrzaja ---------- */
builder.Services.AddSingleton<IContentTargetAudienceRepository, InMemoryContentTargetAudienceRepository>();
builder.Services.AddScoped<SetContentTargetAudienceHandler>();

// DEMO: ovi lookup-i dozvoljavaju sve, zato ih registrujemo samo u Development okruzenju.
// Prava provera vlasnistva dolazi iz modula ST-008/ST-019 (tudji zadaci).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IContentLookupForAudience, DemoContentLookup>();
    builder.Services.AddSingleton<IGroupLookupForAudience, DemoGroupLookupForAudience>();
}
/* -------------------------------------------------------- */

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/demo/courses", async (ICourseReader reader, CancellationToken ct) =>
{
    var courses = await reader.ListAsync(ct);
    return Results.Ok(courses);
});

app.MapGet("/api/courses/{courseId}/content", async (
    Guid courseId,
    Guid studentId,
    Application.Content.GetStudentContentHandler handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    // 1. Provera da li je korisnik ulogovan
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    // 2. Provera da li student pokušava da čita tudji sadržaj
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != studentId)
    {
        return Results.Forbid();
    }

    var query = new Application.Content.GetStudentContentQuery(studentId, courseId);
    var result = await handler.Handle(query, ct);
    return Results.Ok(result);
}).RequireAuthorization();


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

/* ---------- ST-020: endpoint (samo Development / demo) ---------- */
if (app.Environment.IsDevelopment())
{
    app.MapPost("/contents/{contentId:guid}/target-audience", async (
        Guid contentId,
        SetContentTargetAudienceRequest request,
        SetContentTargetAudienceHandler handler,
        HttpContext httpContext,
        CancellationToken ct) =>
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var instructorId))
        {
            return Results.Unauthorized();
        }

        var command = new SetContentTargetAudienceCommand(
            ContentId: contentId,
            InstructorId: instructorId,
            IsForWholeCourseExecution: request.IsForWholeCourseExecution,
            TargetGroupIds: request.TargetGroupIds ?? new List<Guid>()
        );

        try
        {
            await handler.Handle(command, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)         { return Results.BadRequest(ex.Message); }                      // 400
        catch (KeyNotFoundException ex)      { return Results.NotFound(ex.Message); }                        // 404
        catch (UnauthorizedAccessException)  { return Results.StatusCode(StatusCodes.Status403Forbidden); } // 403
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }                        // 409
    })
    .RequireAuthorization();
}
/* ---------------------------------------------------------------- */

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

/* ---------- ST-020: in-memory i demo implementacije ---------- */
public class InMemoryContentTargetAudienceRepository : IContentTargetAudienceRepository
{
    private readonly Dictionary<Guid, Domain.Entities.ContentTargetAudience> _storage = new();

    public Task AddOrUpdateAsync(Domain.Entities.ContentTargetAudience audience, CancellationToken cancellationToken)
    {
        _storage[audience.ContentId] = audience;
        return Task.CompletedTask;
    }

    public Task<Domain.Entities.ContentTargetAudience?> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken)
    {
        _storage.TryGetValue(contentId, out var audience);
        return Task.FromResult(audience);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class DemoContentLookup : IContentLookupForAudience
{
    public Task<bool> ExistsAsync(Guid contentId, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task<Guid> GetCourseExecutionIdAsync(Guid contentId, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid());

    // Za demo svrhe vraćamo true da ne blokira lokalni rad
    public Task<bool> CanInstructorEditContentAsync(Guid contentId, Guid instructorId, CancellationToken cancellationToken) => Task.FromResult(true);
}

public class DemoGroupLookupForAudience : IGroupLookupForAudience
{
    public Task<bool> AllGroupsBelongToCourseExecutionAsync(List<Guid> groupIds, Guid courseExecutionId, CancellationToken cancellationToken) => Task.FromResult(true);
}

public record SetContentTargetAudienceRequest(
    bool IsForWholeCourseExecution,
    List<Guid>? TargetGroupIds
);
/* -------------------------------------------------------------- */

public partial class Program { }
