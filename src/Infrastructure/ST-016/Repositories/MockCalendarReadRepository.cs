using Studium.Domain.ST016.Enums;
using Studium.Domain.ST016.Repositories;
using Studium.Domain.ST016.ValueObjects;

namespace Studium.Infrastructure.ST016.Repositories;

public class MockCalendarReadRepository : ICalendarReadRepository
{
    private readonly Guid AnaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<IEnumerable<CalendarEvent>> GetMonthlyEventsAsync(Guid korisnikId, int godina, int mesec, CancellationToken ct)
    {
        var events = new List<CalendarEvent>();
        var zadatakId = Guid.NewGuid();

        if (korisnikId == AnaId)
        {
            events.Add(new CalendarEvent(zadatakId, "Projekat ST-015", new DateTime(2026, 11, 12), VrstaAktivnosti.Zadatak, $"/zadaci/{zadatakId}"));
        }
        else
        {
            events.Add(new CalendarEvent(zadatakId, "Projekat ST-015", new DateTime(2026, 11, 10), VrstaAktivnosti.Zadatak, $"/zadaci/{zadatakId}"));
        }

        return Task.FromResult(events.AsEnumerable());
    }
}