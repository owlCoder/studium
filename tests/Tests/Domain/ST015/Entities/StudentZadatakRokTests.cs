using NUnit.Framework;
using Studium.Domain.ST015.Entities;

namespace Studium.Tests.Domain.ST015;

[TestFixture]
public class StudentZadatakRokTests
{
    [Test]
    public void IzracunajPrimenljiviRok_ImaPojedinacniRok_VracaPojedinacni()
    {
        // Arrange
        var studentRok = new StudentZadatakRok(Guid.NewGuid(), Guid.NewGuid());
        var zajednickiRok = new DateTime(2026, 11, 10);
        var pojedinacniRok = new DateTime(2026, 11, 12);
        studentRok.PostaviPojedinacniRok(pojedinacniRok);

        // Act
        var rezultat = studentRok.IzracunajPrimenljiviRok(zajednickiRok);

        // Assert
        Assert.That(rezultat, Is.EqualTo(pojedinacniRok));
    }

    [Test]
    public void IzracunajPrimenljiviRok_NemaPojedinacniRok_VracaZajednicki()
    {
        // Arrange
        var studentRok = new StudentZadatakRok(Guid.NewGuid(), Guid.NewGuid());
        var zajednickiRok = new DateTime(2026, 11, 10);

        // Act
        var rezultat = studentRok.IzracunajPrimenljiviRok(zajednickiRok);

        // Assert
        Assert.That(rezultat, Is.EqualTo(zajednickiRok));
    }
}