using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/offerings/{offeringId}/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly EnrollmentService _service;

    public EnrollmentsController(EnrollmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetEnrollments(Guid offeringId)
    {
        if (!TryGetTeacherId(out var teacherId))
        {
            return Unauthorized(new { message = "Korisnik nije autentifikovan." });
        }

        var result = await _service.SearchAsync(offeringId, teacherId);
        if (result == null)
        {
            return StatusCode(403, new { message = "Nemate pravo pristupa ovom izvođenju." });
        }

        return Ok(result);
    }

    [HttpPatch("{enrollmentId}/terminate")]
    public async Task<IActionResult> TerminateEnrollment(Guid offeringId, Guid enrollmentId)
    {
        if (!TryGetTeacherId(out var teacherId))
        {
            return Unauthorized(new { message = "Korisnik nije autentifikovan." });
        }

        var result = await _service.TerminateAsync(offeringId, enrollmentId, teacherId);

        return result switch
        {
            EnrollmentOperationResult.Success => NoContent(),
            EnrollmentOperationResult.Forbidden => StatusCode(403, new { message = "Nemate pravo pristupa ovom izvođenju." }),
            _ => NotFound(new { message = "Članstvo nije pronađeno ili ne pripada ovom izvođenju." })
        };
    }

    private bool TryGetTeacherId(out Guid teacherId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out teacherId) && teacherId != Guid.Empty;
    }
}
