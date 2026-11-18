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
    public async Task Handle_StudentPostoji_UspesnoProduzavaRok()
    {
        // Arrange
        var mockRepo = new Mock<IRokoviRepository>();
        var studentId = Guid.NewGuid();
        var zadatakId = Guid.NewGuid();
        var noviRok = new DateTime(2026, 11, 15);
        
        var studentRok = new StudentZadatakRok(studentId, zadatakId);
        mockRepo.Setup(r => r.GetStudentRokAsync(studentId, zadatakId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(studentRok);

        var handler = new ExtendIndividualDeadlineCommandHandler(mockRepo.Object);
        var command = new ExtendIndividualDeadlineCommand(studentId, zadatakId, noviRok);

        // Act
        var rezultat = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(rezultat, Is.True);
        Assert.That(studentRok.PojedinacniRok, Is.EqualTo(noviRok));
        mockRepo.Verify(r => r.UpdateStudentRokAsync(studentRok), Times.Once);
    }
}