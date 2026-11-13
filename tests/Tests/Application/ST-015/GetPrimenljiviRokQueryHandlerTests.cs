using Moq;
using NUnit.Framework;
using Studium.Application.ST015.Queries;
using Studium.Domain.ST015.Entities;
using Studium.Domain.ST015.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Studium.Tests.Application.ST015;

[TestFixture]
public class GetPrimenljiviRokQueryHandlerTests
{
    private Mock<IRokoviRepository> _repositoryMock;
    private GetPrimenljiviRokQueryHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IRokoviRepository>();
        _handler = new GetPrimenljiviRokQueryHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task Handle_KadaStudentImaPojedinacniRok_VracaPojedinacniRok()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();
        var zajednickiRok = new DateTime(2026, 11, 10);
        var pojedinacniRok = new DateTime(2026, 11, 15);

        var zadatak = new Zadatak(zadatakId, zajednickiRok);
        
        var studentRok = new StudentZadatakRok(studentId, zadatakId);
        studentRok.PostaviPojedinacniRok(pojedinacniRok);

        _repositoryMock.Setup(r => r.GetZadatakAsync(zadatakId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(zadatak);
        _repositoryMock.Setup(r => r.GetStudentRokAsync(studentId, zadatakId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(studentRok);

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId);

        // Act
        var rezultat = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(rezultat, Is.EqualTo(pojedinacniRok));
    }

    [Test]
    public async Task Handle_KadaStudentNemaSvojZapis_VracaZajednickiRok()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();
        var zajednickiRok = new DateTime(2026, 11, 10);

        var zadatak = new Zadatak(zadatakId, zajednickiRok);

        _repositoryMock.Setup(r => r.GetZadatakAsync(zadatakId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(zadatak);
        
        // Vraćamo null jer student nema poseban rok
        _repositoryMock.Setup(r => r.GetStudentRokAsync(studentId, zadatakId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((StudentZadatakRok?)null); 

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId);

        // Act
        var rezultat = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(rezultat, Is.EqualTo(zajednickiRok));
    }

    [Test]
    public void Handle_KadaZadatakNePostoji_BacaArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetZadatakAsync(zadatakId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Zadatak?)null);

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(query, CancellationToken.None));
    }
}