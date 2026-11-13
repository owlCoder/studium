
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

        await _addHandler.Handle(command, cancellationToken);

        return Ok(new { Message = "Student je uspešno dodat u grupu." });
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

        await _removeHandler.Handle(command, cancellationToken);

        return Ok(new { Message = "Članstvo studenta u grupi je uspešno završeno." });
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


    private bool CanManageGroup(Guid groupId)
    {
        // Admin može da upravlja svim grupama.
        if (User.IsInRole("Admin"))
            return true;

        // Nastavnik može da upravlja samo dodeljenom grupom.
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

