using MediatR;
using Studium.Application.ST016.DTOs;

namespace Studium.Application.ST016.Queries;

public sealed record GetMonthlyCalendarQuery(Guid KorisnikId, int Godina, int Mesec) : IRequest<IEnumerable<CalendarEventDto>>;