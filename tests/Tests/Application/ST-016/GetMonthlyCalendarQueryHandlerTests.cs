using Moq;
using NUnit.Framework;
using Studium.Application.ST016.Queries;
using Studium.Domain.ST016.Enums;
using Studium.Domain.ST016.Repositories;
using Studium.Domain.ST016.ValueObjects;

namespace Studium.Tests.Application.ST016;

[TestFixture]
public class GetMonthlyCalendarQueryHandlerTests
{
    [Test]
    public async Task Handle_PretvaraDogadjajeUDto()
    {
        // Arrange
        var mockRepo = new Mock<ICalendarReadRepository>();
        var korisnikId = Guid.NewGuid();
        var dogadjajiIzBaze = new List<CalendarEvent>
        {
            new CalendarEvent(Guid.NewGuid(), "Zadatak 1", new DateTime(2026, 11, 10), VrstaAktivnosti.Zadatak, "/url")
        };

        mockRepo.Setup(r => r.GetMonthlyEventsAsync(korisnikId, 2026, 11, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dogadjajiIzBaze);

        var handler = new GetMonthlyCalendarQueryHandler(mockRepo.Object);

        // Act
        var rezultat = await handler.Handle(new GetMonthlyCalendarQuery(korisnikId, 2026, 11), CancellationToken.None);

        // Assert
        var lista = rezultat.ToList();
        Assert.That(lista.Count, Is.EqualTo(1));
        Assert.That(lista[0].Naziv, Is.EqualTo("Zadatak 1"));
        Assert.That(lista[0].Tip, Is.EqualTo("Zadatak")); // Provera mapiranja Enuma u String
    }
}