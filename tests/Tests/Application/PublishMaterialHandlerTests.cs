using System.Text;
using Moq;
using NUnit.Framework;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;

namespace Tests.Application;

/// <summary>
/// ST-009, US-08: objava materijala.
/// </summary>
[TestFixture]
public sealed class PublishMaterialHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us08-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static InMemoryOfferingStaffChecker StaffOnOffering() =>
        new([(TeacherId, OfferingId)]);

    private static Material Draft() =>
        new(Guid.NewGuid(), OfferingId, "Predavanje 1", "Uvod");

    /// <summary>
    /// Handler bez priloga: skladište je strogi mock, pa svaki neočekivani poziv obara test.
    /// </summary>
    private static PublishMaterialHandler CreateHandler(
        IMaterialRepository materials,
        IOfferingStaffChecker staffChecker,
        IMaterialAttachmentRepository? attachments = null,
        IAttachmentFileStorage? fileStorage = null) =>
        new(
            materials,
            attachments ?? new InMemoryMaterialAttachmentRepository(),
            fileStorage ?? new Mock<IAttachmentFileStorage>(MockBehavior.Strict).Object,
            staffChecker);

    private static Mock<IMaterialRepository> RepositoryWith(Material material)
    {
        var repository = new Mock<IMaterialRepository>();
        repository
            .Setup(x => x.GetByIdAsync(material.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(material);
        return repository;
    }

    private static void VerifyNothingSaved(Mock<IMaterialRepository> repository) =>
        repository.Verify(x => x.UpdateAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    // --- Promena stanja iz nacrta u objavljen (tačka 1) ---

    [Test]
    public async Task HandleAsync_TeacherPublishesDraftAndSavesIt()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var handler = CreateHandler(repository.Object, StaffOnOffering());

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
        repository.Verify(x => x.UpdateAsync(
            It.Is<Material>(m =>
                m.Id == material.Id &&
                m.PublicationState == MaterialPublicationState.Published),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PublishedStateIsStoredInRepository()
    {
        var repository = new InMemoryMaterialRepository();
        var material = Draft();
        await repository.AddAsync(material);
        var handler = CreateHandler(repository, StaffOnOffering());

        await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        var saved = await repository.GetByIdAsync(material.Id);
        Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
    }

    [Test]
    public async Task HandleAsync_PublishesOnlyRequestedMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var target = Draft();
        var other = Draft();
        await repository.AddAsync(target);
        await repository.AddAsync(other);
        var handler = CreateHandler(repository, StaffOnOffering());

        await handler.HandleAsync(new PublishMaterialCommand(TeacherId, target.Id));

        var savedOther = await repository.GetByIdAsync(other.Id);
        Assert.That(savedOther!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
    }

    [Test]
    public async Task HandleAsync_UnknownMaterialReturnsNotFoundWithoutAskingStaffChecker()
    {
        var repository = new Mock<IMaterialRepository>();
        var checker = new Mock<IOfferingStaffChecker>();
        var handler = CreateHandler(repository.Object, checker.Object);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, Guid.NewGuid()));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.MaterialNotFound));
        checker.Verify(x => x.IsStaffOnOfferingAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved(repository);
    }

    [Test]
    public async Task HandleAsync_AlreadyPublishedReturnsNotDraftAndSavesNothing()
    {
        var material = Draft();
        material.Publish();
        var repository = RepositoryWith(material);
        var handler = CreateHandler(repository.Object, StaffOnOffering());

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.NotDraft));
            Assert.That(result.MissingFileNames, Is.Empty);
            Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
        });
        VerifyNothingSaved(repository);
    }

    // --- Provera nastavnog osoblja izvođenja, ST-005 (tačka 1) ---

    [Test]
    public void HandleAsync_RejectsUserWhoIsNotStaffAndSavesNothing()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var handler = CreateHandler(repository.Object, StaffOnOffering());

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.NewGuid(), material.Id)));

        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_RejectsStaffOfAnotherOfferingAndSavesNothing()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var otherOfferingStaff = new InMemoryOfferingStaffChecker([(TeacherId, Guid.NewGuid())]);
        var handler = CreateHandler(repository.Object, otherOfferingStaff);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id)));

        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_RejectsMissingUserWithoutAskingStaffChecker()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var checker = new Mock<IOfferingStaffChecker>();
        var handler = CreateHandler(repository.Object, checker.Object);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.Empty, material.Id)));

        checker.Verify(x => x.IsStaffOnOfferingAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_ChecksAccessBeforeRevealingPublicationState()
    {
        var material = Draft();
        material.Publish();
        var repository = RepositoryWith(material);
        var handler = CreateHandler(repository.Object, StaffOnOffering());

        // Korisnik van osoblja dobija odbijanje pristupa, ne podatak da materijal nije nacrt.
        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.NewGuid(), material.Id)));
    }

    [Test]
    public async Task HandleAsync_AsksStaffCheckerForRequestingUserAndMaterialOffering()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var checker = new Mock<IOfferingStaffChecker>();
        checker.Setup(x => x.IsStaffOnOfferingAsync(TeacherId, OfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(repository.Object, checker.Object);

        await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        checker.Verify(x => x.IsStaffOnOfferingAsync(
            TeacherId, OfferingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Objava odbijena ako neki prilog nema sačuvanu datoteku (tačka 2) ---

    private static MaterialAttachment AttachmentOf(Material material, string fileName, string storageKey) =>
        new(Guid.NewGuid(), material.Id, fileName, "application/pdf", 10, storageKey);

    private async Task<MaterialAttachment> UploadAsync(
        IAttachmentFileStorage storage,
        InMemoryMaterialAttachmentRepository attachments,
        Material material,
        string fileName)
    {
        var key = await storage.SaveAsync(Content("sadrzaj " + fileName));
        var attachment = AttachmentOf(material, fileName, key);
        await attachments.AddAsync(attachment);
        return attachment;
    }

    [Test]
    public async Task HandleAsync_AllAttachmentsHaveStoredFiles_MaterialIsPublished()
    {
        var materials = new InMemoryMaterialRepository();
        var material = Draft();
        await materials.AddAsync(material);
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new LocalDiskAttachmentFileStorage(_root);
        await UploadAsync(storage, attachments, material, "skripta.pdf");
        await UploadAsync(storage, attachments, material, "primeri.zip");
        var handler = CreateHandler(materials, StaffOnOffering(), attachments, storage);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        var saved = await materials.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
            Assert.That(result.MissingFileNames, Is.Empty);
            Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
        });
    }

    [Test]
    public async Task HandleAsync_MaterialWithoutAttachments_IsPublishedWithoutTouchingStorage()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(repository.Object, StaffOnOffering(), fileStorage: storage.Object);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
        storage.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_FileDeletedFromStorage_PublishRejectedAndMaterialStaysDraft()
    {
        var materials = new InMemoryMaterialRepository();
        var material = Draft();
        await materials.AddAsync(material);
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new LocalDiskAttachmentFileStorage(_root);
        await UploadAsync(storage, attachments, material, "skripta.pdf");
        var lost = await UploadAsync(storage, attachments, material, "primeri.zip");
        await storage.DeleteAsync(lost.StorageKey);
        var handler = CreateHandler(materials, StaffOnOffering(), attachments, storage);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        var saved = await materials.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.MissingAttachmentFiles));
            Assert.That(result.MissingFileNames, Is.EqualTo(new[] { "primeri.zip" }));
            Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        });
    }

    [Test]
    public async Task HandleAsync_SeveralFilesMissing_ListsAllMissingNamesAndSavesNothing()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var attachments = new InMemoryMaterialAttachmentRepository();
        await attachments.AddAsync(AttachmentOf(material, "skripta.pdf", "kljuc-1"));
        await attachments.AddAsync(AttachmentOf(material, "primeri.zip", "kljuc-2"));
        var storage = new Mock<IAttachmentFileStorage>();
        storage
            .Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);
        var handler = CreateHandler(repository.Object, StaffOnOffering(), attachments, storage.Object);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.MissingAttachmentFiles));
            Assert.That(result.MissingFileNames, Is.EquivalentTo(new[] { "skripta.pdf", "primeri.zip" }));
            Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        });
        VerifyNothingSaved(repository);
    }

    [Test]
    public async Task HandleAsync_ChecksStorageKeyOfEveryAttachmentAndClosesOpenedContent()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var attachments = new InMemoryMaterialAttachmentRepository();
        await attachments.AddAsync(AttachmentOf(material, "skripta.pdf", "kljuc-1"));
        await attachments.AddAsync(AttachmentOf(material, "primeri.zip", "kljuc-2"));
        var opened = new List<TrackingStream>();
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        storage
            .Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var stream = new TrackingStream();
                opened.Add(stream);
                return stream;
            });
        var handler = CreateHandler(repository.Object, StaffOnOffering(), attachments, storage.Object);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
        storage.Verify(s => s.OpenReadAsync("kljuc-1", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.OpenReadAsync("kljuc-2", It.IsAny<CancellationToken>()), Times.Once);
        storage.VerifyNoOtherCalls();
        Assert.That(opened.All(s => s.IsDisposed), Is.True, "Otvoren sadržaj priloga mora biti zatvoren.");
    }

    [Test]
    public async Task HandleAsync_MissingFileOfAnotherMaterial_DoesNotBlockPublish()
    {
        var materials = new InMemoryMaterialRepository();
        var material = Draft();
        var other = Draft();
        await materials.AddAsync(material);
        await materials.AddAsync(other);
        var attachments = new InMemoryMaterialAttachmentRepository();
        await attachments.AddAsync(AttachmentOf(other, "tudji.pdf", Guid.NewGuid().ToString("N")));
        var handler = CreateHandler(
            materials, StaffOnOffering(), attachments, new LocalDiskAttachmentFileStorage(_root));

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
    }

    [Test]
    public void HandleAsync_UserNotOnStaff_IsRejectedBeforeCheckingAttachments()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var attachments = new Mock<IMaterialAttachmentRepository>(MockBehavior.Strict);
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(repository.Object, StaffOnOffering(), attachments.Object, storage.Object);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.NewGuid(), material.Id)));

        attachments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_AlreadyPublished_DoesNotCheckAttachments()
    {
        var material = Draft();
        material.Publish();
        var repository = RepositoryWith(material);
        var attachments = new Mock<IMaterialAttachmentRepository>(MockBehavior.Strict);
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(repository.Object, StaffOnOffering(), attachments.Object, storage.Object);

        var result = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.NotDraft));
        attachments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_AfterMissingFileIsUploadedAgain_MaterialCanBePublished()
    {
        var materials = new InMemoryMaterialRepository();
        var material = Draft();
        await materials.AddAsync(material);
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var lost = await UploadAsync(storage, attachments, material, "skripta.pdf");
        await storage.DeleteAsync(lost.StorageKey);
        var handler = CreateHandler(materials, StaffOnOffering(), attachments, storage);

        var rejected = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));
        await attachments.RemoveAsync(lost.Id);
        await UploadAsync(storage, attachments, material, "skripta.pdf");
        var accepted = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Outcome, Is.EqualTo(PublishMaterialOutcome.MissingAttachmentFiles));
            Assert.That(accepted.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
        });
    }

    // --- Ulazni podaci i zavisnosti ---

    [Test]
    public void HandleAsync_RejectsNullCommand()
    {
        var handler = CreateHandler(new InMemoryMaterialRepository(), StaffOnOffering());

        Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.HandleAsync(null!));
    }

    [Test]
    public void Constructor_RejectsMissingDependencies()
    {
        var materials = new InMemoryMaterialRepository();
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new Mock<IAttachmentFileStorage>().Object;
        var checker = StaffOnOffering();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new PublishMaterialHandler(null!, attachments, storage, checker));
            Assert.Throws<ArgumentNullException>(() => new PublishMaterialHandler(materials, null!, storage, checker));
            Assert.Throws<ArgumentNullException>(() => new PublishMaterialHandler(materials, attachments, null!, checker));
            Assert.Throws<ArgumentNullException>(() => new PublishMaterialHandler(materials, attachments, storage, null!));
        });
    }

    private sealed class TrackingStream : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
