namespace Studium.Domain.ST016.Repositories;

using Studium.Domain.ST016.ValueObjects;

public interface ICalendarReadRepository
{
    Task<IEnumerable<CalendarEvent>> GetMonthlyEventsAsync(Guid korisnikId, int godina, int mesec, CancellationToken ct);
}