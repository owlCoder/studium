using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studium.Application.ST015.Commands;

namespace Studium.Api.Controllers;

public sealed record ExtendIndividualDeadlineRequest(
    Guid StudentId,
    Guid ZadatakId,
    DateTime NoviRok);

[ApiController]
[Authorize]
[Route("api/nastavnik/rokovi")]
public class NastavnikRokoviController(ISender _sender) : ControllerBase
{
    [HttpPut("pojedinacni")]
    public async Task<IActionResult> ProduziPojedinacniRok(
        [FromBody] ExtendIndividualDeadlineRequest request,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var aktorId))
            return Unauthorized();

        var jeNastavnik = User.IsInRole("Nastavnik");

        try
        {
            var command = new ExtendIndividualDeadlineCommand(
                aktorId,
                jeNastavnik,
                request.StudentId,
                request.ZadatakId,
                request.NoviRok);

            await _sender.Send(command, ct);
            return Ok("Rok uspešno produžen.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}