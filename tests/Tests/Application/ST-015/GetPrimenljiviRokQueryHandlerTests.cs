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
    private Mock<IRokoviRepository> _repositoryMock = null!;
    private GetPrimenljiviRokQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IRokoviRepository>();
        _handler = new GetPrimenljiviRokQueryHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task Handle_StudentNemaProduzenRok_VracaZajednickiRok()
    {
        var zadatakId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var zajednickiRok = DateTime.UtcNow.AddDays(7);

        var zadatak = new Zadatak(zadatakId, zajednickiRok);

        _repositoryMock
            .Setup(r => r.GetZadatakAsync(zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zadatak);

        _repositoryMock
            .Setup(r => r.GetStudentRokAsync(studentId, zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentZadatakRok?)null);

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId, studentId, false);
        var rezultat = await _handler.Handle(query, CancellationToken.None);

        Assert.That(rezultat, Is.EqualTo(zajednickiRok));
    }

    [Test]
    public void Handle_ZadatakNePostoji_BacaArgumentException()
    {
        var zadatakId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetZadatakAsync(zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Zadatak?)null);

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId, studentId, false);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public void Handle_DrugiStudentPokusavaPristup_BacaUnauthorizedAccessException()
    {
        var zadatakId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var drugiStudentId = Guid.NewGuid();

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId, drugiStudentId, false);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public void Handle_NeovlascenNastavnikPokusavaPristup_BacaUnauthorizedAccessException()
    {
        var zadatakId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var neovlascenNastavnikId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(neovlascenNastavnikId, zadatakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetPrimenljiviRokQuery(studentId, zadatakId, neovlascenNastavnikId, true);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(query, CancellationToken.None));
    }
}