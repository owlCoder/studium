using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Application.Services;

namespace Api.Controllers;


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
    public async Task<IActionResult> TerminateEnrollment(Guid enrollmentId)
    {
        var success = await _service.TerminateAsync(enrollmentId);
        return success ? NoContent() : NotFound();
    }
}
