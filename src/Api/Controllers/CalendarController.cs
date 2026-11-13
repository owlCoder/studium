using MediatR;
using Microsoft.AspNetCore.Mvc;
using Studium.Application.ST016.Queries;

namespace Studium.Api.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController(IMediator _mediator) : ControllerBase
{
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyCalendar(
        [FromQuery] Guid korisnikId, 
        [FromQuery] int godina, 
        [FromQuery] int mesec)
    {
        var query = new GetMonthlyCalendarQuery(korisnikId, godina, mesec);
        var result = await _mediator.Send(query);
        
        return Ok(result);
    }
}