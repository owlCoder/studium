using Application.Enrollments;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Enrollments;

[TestFixture]
public sealed class ListStudentOfferingsHandlerTests
{
    [Test]
    public async Task HandleAsync_SortiraPoGodiniPaPoNazivu()
    {
        var studentId = Guid.NewGuid();
        IReadOnlyList<EnrolledOfferingDto> rows = new[]
        {
            new EnrolledOfferingDto(Guid.NewGuid(), "Osnove informacione bezbednosti", "2026/27"),
            new EnrolledOfferingDto(Guid.NewGuid(), "Baze podataka", "2025/26"),
            new EnrolledOfferingDto(Guid.NewGuid(), "Elementi razvoja softvera", "2026/27")
        };

        var reader = new Mock<IStudentOfferingsReader>();
        reader.Setup(x => x.ListActiveForStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var result = await new ListStudentOfferingsHandler(reader.Object).HandleAsync(studentId);

        Assert.That(result.Select(x => x.CourseName), Is.EqualTo(new[]
        {
            "Elementi razvoja softvera",
            "Osnove informacione bezbednosti",
            "Baze podataka"
        }));
        reader.Verify(x => x.ListActiveForStudentAsync(studentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_UklanjaDuplikateIstogIzvodjenja()
    {
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        IReadOnlyList<EnrolledOfferingDto> rows = new[]
        {
            new EnrolledOfferingDto(offeringId, "Elementi razvoja softvera", "2026/27"),
            new EnrolledOfferingDto(offeringId, "Elementi razvoja softvera", "2026/27")
        };

        var reader = new Mock<IStudentOfferingsReader>();
        reader.Setup(x => x.ListActiveForStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var result = await new ListStudentOfferingsHandler(reader.Object).HandleAsync(studentId);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void HandleAsync_OdbijaPrazanIdStudenta()
    {
        var reader = new Mock<IStudentOfferingsReader>();
        var handler = new ListStudentOfferingsHandler(reader.Object);

        Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(Guid.Empty));
        reader.Verify(x => x.ListActiveForStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
