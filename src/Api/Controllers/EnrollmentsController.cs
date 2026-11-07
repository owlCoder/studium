using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        var result = await _service.SearchAsync(offeringId);
        return Ok(result);
    }

    [HttpPatch("{enrollmentId}/terminate")]
    public async Task<IActionResult> TerminateEnrollment(Guid offeringId, Guid enrollmentId)
    {

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid teacherId))
        {
            return Unauthorized(new { message = "Korisnik nije autentifikovan." });
        }


        var success = await _service.TerminateAsync(offeringId, enrollmentId, teacherId);

        return success ? NoContent() : NotFound(new { message = "Članstvo nije pronađeno ili ne pripada ovom izvođenju." });
    }
}
