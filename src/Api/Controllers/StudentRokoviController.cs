using MediatR;
using Microsoft.AspNetCore.Mvc;
using Studium.Application.ST015.Queries;
using System;
using System.Threading.Tasks;

namespace Studium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var query = new GetPrimenljiviRokQuery(studentId, zadatakId);
        var primenljiviRok = await _sender.Send(query);
        
        return Ok(new { PrimenljiviRok = primenljiviRok });
    }
}