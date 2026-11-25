using Application.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/offerings/{offeringId}/assignments")]
public sealed class AssignmentsController : ControllerBase
{
    private readonly CreateAssignmentHandler _handler;

    public AssignmentsController(CreateAssignmentHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid offeringId, CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAssignmentCommand(
            offeringId, request.Title, request.Description, request.Instructions, request.Deadline);

        try
        {
            var assignmentId = await _handler.HandleAsync(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { assignmentId });
        }
        catch (ArgumentException exception) when (
            exception.ParamName is nameof(command.CourseOfferingId) or nameof(command.Title))
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Nemate pravo da kreirate zadatak za ovo izvođenje." });
        }
    }

    public sealed record CreateAssignmentRequest(
        string? Title, string? Description, string? Instructions, DateTimeOffset? Deadline);
}
