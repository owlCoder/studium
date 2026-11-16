using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Application.Groups;
using Application;
 
namespace Api.Controllers;
 
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "GroupOwnerPolicy")]
public class GroupsController : ControllerBase
{
    private readonly AddStudentToGroupHandler _addHandler;
    private readonly RemoveStudentFromGroupHandler _removeHandler;
    private readonly ListStudentGroupsHandler _listHandler;
 
    public GroupsController(
        AddStudentToGroupHandler addHandler,
        RemoveStudentFromGroupHandler removeHandler,
        ListStudentGroupsHandler listHandler)
    {
        _addHandler = addHandler;
        _removeHandler = removeHandler;
        _listHandler = listHandler;
    }
 
    [HttpPost("{groupId}/students")]
    public async Task<IActionResult> AddStudent(
        Guid groupId,
        [FromBody] AddStudentRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanManageGroup(groupId))
            return Forbid();
 
        var command = new AddStudentToGroupCommand(
            groupId,
            request.StudentId,
            request.StartDate);
 
        return await ExecuteAsync(
            () => _addHandler.Handle(command, cancellationToken),
            "Student je uspešno dodat u grupu.");
    }
 
    [HttpDelete("{groupId}/students/{studentId}")]
    public async Task<IActionResult> RemoveStudent(
        Guid groupId,
        Guid studentId,
        [FromBody] RemoveStudentRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanManageGroup(groupId))
            return Forbid();
 
        var command = new RemoveStudentFromGroupCommand(
            groupId,
            studentId,
            request.EndDate);
 
        return await ExecuteAsync(
            () => _removeHandler.Handle(command, cancellationToken),
            "Članstvo studenta u grupi je uspešno završeno.");
    }
 
    [HttpGet("students/{studentId}/groups")]
    public async Task<IActionResult> GetStudentGroups(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (!CanViewStudentGroups(studentId))
            return Forbid();
 
        var groups = await _listHandler.HandleAsync(
            studentId,
            cancellationToken);
 
        return Ok(groups);
    }
 
    private async Task<IActionResult> ExecuteAsync(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            return Ok(new { Message = successMessage });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
 
    private bool CanManageGroup(Guid groupId)
    {
        if (User.IsInRole("Admin"))
            return true;
 
        var allowedGroupClaim = User.FindFirst("AllowedGroup");
 
        return allowedGroupClaim != null &&
               Guid.TryParse(allowedGroupClaim.Value, out var allowedId) &&
               allowedId == groupId;
    }
 
    private bool CanViewStudentGroups(Guid studentId)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }
 
        var studentClaim =
            User.FindFirst(ClaimTypes.NameIdentifier) ??
            User.FindFirst("sub") ??
            User.FindFirst("StudentId");
 
        return studentClaim != null &&
               Guid.TryParse(studentClaim.Value, out var loggedStudentId) &&
               loggedStudentId == studentId;
    }
}
 
public record AddStudentRequest(Guid StudentId, DateTime StartDate);
 
public record RemoveStudentRequest(DateTime EndDate);