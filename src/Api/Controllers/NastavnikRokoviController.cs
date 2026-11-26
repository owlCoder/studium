using MediatR;
using Microsoft.AspNetCore.Mvc;
using Studium.Application.ST015.Commands;

namespace Studium.Api.Controllers;

[ApiController]
[Route("api/nastavnik/rokovi")]
public class NastavnikRokoviController(IMediator _mediator) : ControllerBase
{
    [HttpPut("pojedinacni")]
    public async Task<IActionResult> ProduziPojedinacniRok([FromBody] ExtendIndividualDeadlineCommand command)
    {
        var result = await _mediator.Send(command);
        return result ? Ok("Rok uspešno produžen.") : NotFound();
    }
}