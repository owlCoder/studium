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
using Application.ContentAudience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

builder.Services.AddSingleton<IGroupMembershipRepository, InMemoryGroupMembershipRepository>();
builder.Services.AddSingleton<IGroupLookup, DemoGroupLookup>();
builder.Services.AddSingleton<IStudentLookup, DemoStudentLookup>();

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

/*
--------------------------------------
*/

builder.Services.AddSingleton<IContentTargetAudienceRepository, InMemoryContentTargetAudienceRepository>();
builder.Services.AddSingleton<IContentLookupForAudience, DemoContentLookup>();
builder.Services.AddSingleton<IGroupLookupForAudience, DemoGroupLookupForAudience>();

builder.Services.AddScoped<SetContentTargetAudienceHandler>();
/*------------------------------------*/

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

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

/*---------------------*/
app.MapPost("/contents/{contentId}/target-audience", async (
    Guid contentId,
    [FromBody] SetTargetAudienceRequest request,
    SetContentTargetAudienceHandler handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    try
    {
        var command = new SetContentTargetAudienceCommand(contentId, request.IsForWholeCourseExecution, request.TargetGroupIds);
        await handler.Handle(command, ct);
        return Results.Ok(new { message = "Publika nastavnog sadržaja je uspešno definisana." });
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
});
/*---------------------*/

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

app.Run();

public class DemoGroupLookup : IGroupLookup
{
    public Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken) => Task.FromResult(true);
}

public class DemoStudentLookup : IStudentLookup
{
    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken) => Task.FromResult(true);
}

// --- In-memory i demo implementacije za ST-020 ---

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
}

public class DemoGroupLookupForAudience : IGroupLookupForAudience
{
    public Task<bool> AllGroupsBelongToCourseExecutionAsync(List<Guid> groupIds, Guid courseExecutionId, CancellationToken cancellationToken) => Task.FromResult(true);
}

public record AddStudentRequest(Guid StudentId, DateTime StartDate);
public record RemoveStudentRequest(DateTime EndDate);

/*---------------------*/
public record SetTargetAudienceRequest(bool IsForWholeCourseExecution, List<Guid>? TargetGroupIds);
/*---------------------*/

public partial class Program { }