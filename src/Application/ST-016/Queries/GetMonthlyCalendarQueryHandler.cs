using MediatR;
using Studium.Application.ST016.DTOs;
using Studium.Domain.ST016.Repositories;

namespace Studium.Application.ST016.Queries;

public class GetMonthlyCalendarQueryHandler(ICalendarReadRepository _repository) 
    : IRequestHandler<GetMonthlyCalendarQuery, IEnumerable<CalendarEventDto>>
{
    public async Task<IEnumerable<CalendarEventDto>> Handle(GetMonthlyCalendarQuery request, CancellationToken ct)
    {
        var events = await _repository.GetMonthlyEventsAsync(request.KorisnikId, request.Godina, request.Mesec, ct);
        
        return events.Select(e => new CalendarEventDto(
            e.Naziv,
            e.Vreme,
            e.Vrsta.ToString(),
            e.UrlVeze
        ));
    }
}