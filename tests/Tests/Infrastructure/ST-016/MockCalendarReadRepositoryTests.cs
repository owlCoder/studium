using NUnit.Framework;
using Studium.Infrastructure.ST016.Repositories;

namespace Studium.Tests.Infrastructure.ST016;

[TestFixture]
public class MockCalendarReadRepositoryTests
{
    [Test]
    public async Task GetMonthlyEventsAsync_AnaKorisnik_VracaProduzenRok()
    {
        // Arrange
        var repo = new MockCalendarReadRepository();
        var anaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var dogadjaji = await repo.GetMonthlyEventsAsync(anaId, 2026, 11, CancellationToken.None);

        // Assert
        var prviDogadjaj = dogadjaji.First();
        Assert.That(prviDogadjaj.Vreme.Day, Is.EqualTo(12)); // Ана има рок до 12.
    }

    [Test]
    public async Task GetMonthlyEventsAsync_ObicanKorisnik_VracaZajednickiRok()
    {
        // Arrange
        var repo = new MockCalendarReadRepository();
        var obicanId = Guid.NewGuid(); // Било који други ИД

        // Act
        var dogadjaji = await repo.GetMonthlyEventsAsync(obicanId, 2026, 11, CancellationToken.None);

        // Assert
        var prviDogadjaj = dogadjaji.First();
        Assert.That(prviDogadjaj.Vreme.Day, Is.EqualTo(10)); // Остали имају рок до 10.
    }
}