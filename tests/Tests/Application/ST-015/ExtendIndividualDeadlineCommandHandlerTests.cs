using Moq;
using NUnit.Framework;
using Studium.Application.ST015.Commands;
using Studium.Domain.ST015.Entities;
using Studium.Domain.ST015.Repositories;

namespace Studium.Tests.Application.ST015;

[TestFixture]
public class ExtendIndividualDeadlineCommandHandlerTests
{
    [Test]
    public async Task Handle_ValidanNastavnikIStudentPostoji_UspesnoProduzavaRok()
    {
        var mockRepo = new Mock<IRokoviRepository>();
        var nastavnikId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();
        var noviRok = new DateTime(2026, 11, 15);

        mockRepo.Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(nastavnikId, zadatakId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var studentRok = new StudentZadatakRok(studentId, zadatakId);
        mockRepo.Setup(r => r.GetStudentRokAsync(studentId, zadatakId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(studentRok);

        var handler = new ExtendIndividualDeadlineCommandHandler(mockRepo.Object);
        var command = new ExtendIndividualDeadlineCommand(nastavnikId, true, studentId, zadatakId, noviRok);

        await handler.Handle(command, CancellationToken.None);

        Assert.That(studentRok.PojedinacniRok, Is.EqualTo(noviRok));
        mockRepo.Verify(r => r.UpdateStudentRokAsync(studentRok, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_NeovlasceniKorisnik_BacaUnauthorizedAccessExceptionINeMenjaStanje()
    {
        var mockRepo = new Mock<IRokoviRepository>();
        var nastavnikId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();
        var noviRok = new DateTime(2026, 11, 15);

        mockRepo.Setup(r => r.DaLiNastavnikImaPravoNadZadatkomAsync(nastavnikId, zadatakId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var handler = new ExtendIndividualDeadlineCommandHandler(mockRepo.Object);
        var command = new ExtendIndividualDeadlineCommand(nastavnikId, true, studentId, zadatakId, noviRok);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        mockRepo.Verify(r => r.GetStudentRokAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRepo.Verify(r => r.UpdateStudentRokAsync(It.IsAny<StudentZadatakRok>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}