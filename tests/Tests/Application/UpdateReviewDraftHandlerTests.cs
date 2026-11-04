namespace Studium.Tests.Application;

using Moq;
using NUnit.Framework;
using Studium.Application.Reviews;
using Studium.Domain.Reviews;

[TestFixture]
public class UpdateReviewDraftHandlerTests
{
    private Mock<IReviewRepository> _repositoryMock;
    private UpdateReviewDraftHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IReviewRepository>();
        _handler = new UpdateReviewDraftHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenDraftExistsAndIsOwnedByTeacher_UpdatesCommentAndOutcome()
    {
        // Arrange
        var draftId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var existingDraft = new ReviewDraft(draftId, Guid.NewGuid(), teacherId, "Stari komentar", "NeedsRevision");

        _repositoryMock.Setup(r => r.GetByIdAsync(draftId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(existingDraft);

        var command = new UpdateReviewDraftCommand(draftId, teacherId, "Ažurirani komentar nastavnika", "Approved");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.That(existingDraft.Comment, Is.EqualTo("Ažurirani komentar nastavnika"));
        Assert.That(existingDraft.Outcome, Is.EqualTo("Approved"));
        _repositoryMock.Verify(r => r.UpdateAsync(existingDraft, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_WhenDraftDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var draftId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(draftId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((ReviewDraft?)null);

        var command = new UpdateReviewDraftCommand(draftId, Guid.NewGuid(), "Komentar", "Approved");

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _handler.HandleAsync(command));
    }
}