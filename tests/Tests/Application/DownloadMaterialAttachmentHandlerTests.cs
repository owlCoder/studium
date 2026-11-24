using System.Collections.Concurrent;
using System.Text;
using Application.Access;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, „Zaštita pristupa materijalu i prilozima”, tačka 1:
/// provera članstva pri svakom preuzimanju priloga (prilog nasleđuje pravo materijala).
/// </summary>
[TestFixture]
public sealed class DownloadMaterialAttachmentHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherOfferingStudentId = Guid.NewGuid();
    private static readonly Guid EndedStudentId = Guid.NewGuid();
    private static readonly byte[] FileBytes = Encoding.UTF8.GetBytes("sadrzaj skripte");

    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private InMemoryAttachmentFileStorage _storage = null!;
    private Material _material = null!;
    private MaterialAttachment _attachment = null!;

    [SetUp]
    public async Task SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _storage = new InMemoryAttachmentFileStorage();

        // Tačka 2: student vidi samo objavljen materijal, pa je osnovni materijal objavljen.
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        _material.Publish();
        await _materials.AddAsync(_material);
        _attachment = await AddAttachmentAsync(_material.Id);
    }

    private async Task<(Material Draft, MaterialAttachment Attachment)> AddDraftWithAttachmentAsync()
    {
        var draft = new Material(Guid.NewGuid(), _material.CourseOfferingId, "Nacrt vežbi", null);
        await _materials.AddAsync(draft);
        return (draft, await AddAttachmentAsync(draft.Id));
    }

    private async Task<MaterialAttachment> AddAttachmentAsync(Guid materialId, bool withContent = true)
    {
        var key = withContent
            ? await _storage.SaveAsync(new MemoryStream(FileBytes))
            : Guid.NewGuid().ToString("N");
        var attachment = new MaterialAttachment(
            Guid.NewGuid(), materialId, "skripta.pdf", "application/pdf", FileBytes.Length, key);
        await _attachments.AddAsync(attachment);
        return attachment;
    }

    private InMemoryMembershipReader Memberships() =>
        new(
        [
            new OfferingMembership(StudentId, _material.CourseOfferingId, MembershipKind.Student, true),
            new OfferingMembership(OtherOfferingStudentId, Guid.NewGuid(), MembershipKind.Student, true),
            new OfferingMembership(EndedStudentId, _material.CourseOfferingId, MembershipKind.Student, false)
        ]);

    private DownloadMaterialAttachmentHandler CreateHandler(
        IMembershipReader? memberships = null,
        IOfferingStaffChecker? staffChecker = null,
        IMaterialAttachmentRepository? attachments = null,
        IAttachmentFileStorage? storage = null) =>
        new(
            _materials,
            attachments ?? _attachments,
            storage ?? _storage,
            staffChecker ?? new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]),
            memberships ?? Memberships());

    private DownloadMaterialAttachmentCommand Command(Guid userId, Guid? attachmentId = null, Guid? materialId = null) =>
        new(userId, materialId ?? _material.Id, attachmentId ?? _attachment.Id);

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        stream.Dispose();
        return copy.ToArray();
    }

    // --- Dozvoljen pristup ---

    [Test]
    public async Task HandleAsync_ActiveStudentOfOffering_GetsAttachmentContent()
    {
        var result = await CreateHandler().HandleAsync(Command(StudentId));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.Found));
            Assert.That(result.Attachment!.Id, Is.EqualTo(_attachment.Id));
            Assert.That(ReadAll(result.Content!), Is.EqualTo(FileBytes));
        });
    }

    [Test]
    public async Task HandleAsync_StaffOfOffering_GetsAttachmentWithoutStudentMembership()
    {
        var memberships = new Mock<IMembershipReader>(MockBehavior.Strict);

        var result = await CreateHandler(memberships.Object).HandleAsync(Command(TeacherId));

        Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.Found));
        result.Content!.Dispose();
    }

    // --- Odbijen pristup: prilog nasleđuje pravo materijala ---

    [Test]
    public void HandleAsync_StudentOfAnotherOffering_IsDeniedWithoutLookingUpAttachment()
    {
        var attachments = new Mock<IMaterialAttachmentRepository>(MockBehavior.Strict);
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(attachments: attachments.Object, storage: storage.Object);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await handler.HandleAsync(Command(OtherOfferingStudentId)));
    }

    [Test]
    public void HandleAsync_StudentWithEndedMembership_IsDenied()
    {
        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await CreateHandler().HandleAsync(Command(EndedStudentId)));
    }

    [Test]
    public void HandleAsync_UnknownUser_IsDenied()
    {
        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await CreateHandler().HandleAsync(Command(Guid.NewGuid())));
    }

    [Test]
    public void HandleAsync_EmptyUser_IsDeniedWithoutAskingAccessPorts()
    {
        var memberships = new Mock<IMembershipReader>(MockBehavior.Strict);
        var staff = new Mock<IOfferingStaffChecker>(MockBehavior.Strict);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await CreateHandler(memberships.Object, staff.Object).HandleAsync(Command(Guid.Empty)));
    }

    [Test]
    public void HandleAsync_DeniedRequestForUnknownAttachment_GetsDenialNotNotFound()
    {
        // Korisnik bez prava ne sme da sazna da li prilog postoji.
        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await CreateHandler().HandleAsync(Command(OtherOfferingStudentId, Guid.NewGuid())));
    }

    // --- Provera pri svakom preuzimanju ---

    [Test]
    public async Task HandleAsync_ChecksMembershipOnEveryDownload()
    {
        var memberships = new Mock<IMembershipReader>();
        memberships
            .SetupSequence(m => m.IsActiveStudentAsync(StudentId, _material.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var handler = CreateHandler(memberships.Object);

        var first = await handler.HandleAsync(Command(StudentId));
        first.Content!.Dispose();

        Assert.That(first.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.Found));
        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await handler.HandleAsync(Command(StudentId)));
        memberships.Verify(
            m => m.IsActiveStudentAsync(StudentId, _material.CourseOfferingId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task HandleAsync_ChecksMembershipOnOfferingOfMaterial()
    {
        var memberships = new Mock<IMembershipReader>();
        memberships
            .Setup(m => m.IsActiveStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler(memberships.Object).HandleAsync(Command(StudentId));
        result.Content!.Dispose();

        memberships.Verify(
            m => m.IsActiveStudentAsync(StudentId, _material.CourseOfferingId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- Tačka 2: nacrt i povučen materijal nedostupni studentu i preko direktne adrese ---

    [Test]
    public async Task HandleAsync_StudentRequestsDraftByDirectAddress_GetsMaterialNotFound()
    {
        var (draft, attachment) = await AddDraftWithAttachmentAsync();

        var result = await CreateHandler().HandleAsync(Command(StudentId, attachment.Id, draft.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.MaterialNotFound));
            Assert.That(result.Attachment, Is.Null);
            Assert.That(result.Content, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_StudentDraft_LooksSameAsUnknownMaterial()
    {
        // Student ne sme da razlikuje „nacrt postoji” od „materijal ne postoji”.
        var (draft, attachment) = await AddDraftWithAttachmentAsync();
        var handler = CreateHandler();

        var draftResult = await handler.HandleAsync(Command(StudentId, attachment.Id, draft.Id));
        var unknownResult = await handler.HandleAsync(Command(StudentId, attachment.Id, Guid.NewGuid()));

        Assert.That(draftResult.Outcome, Is.EqualTo(unknownResult.Outcome));
    }

    [Test]
    public async Task HandleAsync_StudentDraft_DoesNotLookUpAttachmentOrOpenFile()
    {
        var (draft, attachment) = await AddDraftWithAttachmentAsync();
        var attachments = new Mock<IMaterialAttachmentRepository>(MockBehavior.Strict);
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);

        var result = await CreateHandler(attachments: attachments.Object, storage: storage.Object)
            .HandleAsync(Command(StudentId, attachment.Id, draft.Id));

        Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.MaterialNotFound));
    }

    [Test]
    public async Task HandleAsync_StaffDownloadsFromDraft()
    {
        // Nastavnik priprema nacrt i mora moći da proveri svoje priloge pre objave.
        var (draft, attachment) = await AddDraftWithAttachmentAsync();

        var result = await CreateHandler().HandleAsync(Command(TeacherId, attachment.Id, draft.Id));

        Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.Found));
        result.Content!.Dispose();
    }

    [Test]
    public async Task HandleAsync_NonMemberRequestingDraft_IsStillDenied()
    {
        var (draft, attachment) = await AddDraftWithAttachmentAsync();

        Assert.ThrowsAsync<MaterialAccessDeniedException>(
            async () => await CreateHandler().HandleAsync(Command(OtherOfferingStudentId, attachment.Id, draft.Id)));
    }

    [Test]
    public async Task HandleAsync_DraftBecomesAvailableToStudentAfterPublish()
    {
        var (draft, attachment) = await AddDraftWithAttachmentAsync();
        var handler = CreateHandler();

        var before = await handler.HandleAsync(Command(StudentId, attachment.Id, draft.Id));
        draft.Publish();
        var after = await handler.HandleAsync(Command(StudentId, attachment.Id, draft.Id));
        after.Content?.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(before.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.MaterialNotFound));
            Assert.That(after.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.Found));
        });
    }

    // --- Nepostojeći zapisi ---

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsMaterialNotFound()
    {
        var result = await CreateHandler().HandleAsync(Command(StudentId, materialId: Guid.NewGuid()));

        Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.MaterialNotFound));
    }

    [Test]
    public async Task HandleAsync_UnknownAttachment_ReturnsAttachmentNotFound()
    {
        var result = await CreateHandler().HandleAsync(Command(StudentId, Guid.NewGuid()));

        Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.AttachmentNotFound));
    }

    [Test]
    public async Task HandleAsync_AttachmentOfAnotherMaterial_IsNotServedThroughThisMaterial()
    {
        // Student ima pravo nad ovim materijalom, ali traži prilog tuđeg materijala
        // (npr. materijala drugog izvođenja) preko adrese ovog materijala.
        var foreignMaterial = new Material(Guid.NewGuid(), Guid.NewGuid(), "Tuđi materijal", null);
        await _materials.AddAsync(foreignMaterial);
        var foreignAttachment = await AddAttachmentAsync(foreignMaterial.Id);

        var result = await CreateHandler().HandleAsync(Command(StudentId, foreignAttachment.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.AttachmentNotFound));
            Assert.That(result.Content, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_MissingFileContent_ReturnsContentMissing()
    {
        var withoutContent = await AddAttachmentAsync(_material.Id, withContent: false);

        var result = await CreateHandler().HandleAsync(Command(StudentId, withoutContent.Id));

        Assert.That(result.Outcome, Is.EqualTo(DownloadMaterialAttachmentOutcome.ContentMissing));
    }

    [Test]
    public void Constructor_RejectsMissingMembershipReader()
    {
        Assert.That(
            () => new DownloadMaterialAttachmentHandler(
                _materials, _attachments, _storage, new InMemoryOfferingStaffChecker(), null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    /// <summary>
    /// Jednostavno memorijsko skladište sadržaja za testove.
    /// </summary>
    internal sealed class InMemoryAttachmentFileStorage : IAttachmentFileStorage
    {
        private readonly ConcurrentDictionary<string, byte[]> _files = new();

        public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
        {
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            var key = Guid.NewGuid().ToString("N");
            _files[key] = copy.ToArray();
            return key;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(_files.TryGetValue(storageKey, out var bytes) ? new MemoryStream(bytes) : null);

        public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_files.TryRemove(storageKey, out _));
    }
}
