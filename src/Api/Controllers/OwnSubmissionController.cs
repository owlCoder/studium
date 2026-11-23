using Application.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/assignments/{assignmentId}/submissions/me")]
public sealed class OwnSubmissionController : ControllerBase
{
    private readonly GetOwnSubmissionHandler _handler;

    public OwnSubmissionController(GetOwnSubmissionHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromRoute] Guid assignmentId)
    {
        if (assignmentId == Guid.Empty)
            return BadRequest(new { message = "Identifikator zadatka nije ispravan." });

        try
        {
            var submission = await _handler.HandleAsync(assignmentId);
            if (submission == null)
                return NotFound(new { message = "Nemaš predaju za ovaj zadatak." });

            return Ok(new
            {
                submission.Text,
                submission.SubmittedAt,
                Attachments = submission.Attachments.Select(attachment => new
                {
                    attachment.FileName,
                    attachment.FileType,
                    attachment.Size
                })
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
