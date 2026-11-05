using Microsoft.AspNetCore.Mvc;
using Application.Groups;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly AddStudentToGroupHandler _addHandler;
    private readonly RemoveStudentFromGroupHandler _removeHandler;

    public GroupsController(AddStudentToGroupHandler addHandler, RemoveStudentFromGroupHandler removeHandler)
    {
        _addHandler = addHandler;
        _removeHandler = removeHandler;
    }

    [HttpPost("{groupId}/students")]
    public async Task<IActionResult> AddStudent(Guid groupId, [FromBody] AddStudentRequest request, CancellationToken cancellationToken)
    {
        var command = new AddStudentToGroupCommand(groupId, request.StudentId, request.StartDate);
        await _addHandler.Handle(command, cancellationToken);
        return Ok(new { Message = "Student je uspešno dodat u grupu." });
    }

    [HttpDelete("{groupId}/students/{studentId}")]
    public async Task<IActionResult> RemoveStudent(Guid groupId, Guid studentId, [FromBody] RemoveStudentRequest request, CancellationToken cancellationToken)
    {
        var command = new RemoveStudentFromGroupCommand(groupId, studentId, request.EndDate);
        await _removeHandler.Handle(command, cancellationToken);
        return Ok(new { Message = "Članstvo studenta u grupi je uspešno završeno." });
    }
}

public record AddStudentRequest(Guid StudentId, DateTime StartDate);
public record RemoveStudentRequest(DateTime EndDate);