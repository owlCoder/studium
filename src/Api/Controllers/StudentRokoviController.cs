using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studium.Application.ST015.Queries;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Studium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentRokoviController : ControllerBase
{
    private readonly ISender _sender;

    public StudentRokoviController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("zadatci/{zadatakId:guid}/studenti/{studentId:guid}/primenljivi-rok")]
    public async Task<IActionResult> GetPrimenljiviRok(Guid zadatakId, Guid studentId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var akterId))
        {
            return Unauthorized();
        }

        var jeNastavnik = User.IsInRole("Nastavnik");

        try
        {
            var query = new GetPrimenljiviRokQuery(studentId, zadatakId, akterId, jeNastavnik);
            var primenljiviRok = await _sender.Send(query);

            return Ok(new { PrimenljiviRok = primenljiviRok });
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