using System.Text;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-09 (tačka 2): povučen materijal ostaje vidljiv nastavniku
/// i može se ponovo objaviti.
/// </summary>
[TestFixture]
public sealed class WithdrawnMaterialRepublishTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private string _root = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private LocalDiskAttachmentFileStorage _storage = null!;
    private InMemoryOfferingStaffChecker _staff = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us09-" + Guid.NewGuid().ToString("N"));
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _storage = new LocalDiskAttachmentFileStorage(_root);
        _staff = new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)]);

        _material = new Material(Guid.NewGuid(), OfferingId, "Predavanje 1", "Uvod");
        await _materials.AddAsync(_material);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private PublishMaterialHandler PublishHandler() =>
        new(_materials, _attachments, _storage, _staff);

    private WithdrawMaterialHandler WithdrawHandler() => new(_materials, _staff);

    private ListPublishedMaterialsForStudentHandler StudentListHandler() =>
        new(_materials, new InMemoryMembershipReader(
            [new OfferingMembership(StudentId, OfferingId, MembershipKind.Student, IsActive: true)]));

    private async Task PublishAndWithdrawAsync()
    {
        await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));
        await WithdrawHandler().HandleAsync(new WithdrawMaterialCommand(TeacherId, _material.Id));
    }

    private async Task<MaterialAttachment> AddAttachmentAsync(string fileName, bool withFile)
    {
        var key = withFile
            ? await _storage.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("sadrzaj")))
            : Guid.NewGuid().ToString("N"); // ključ bez sačuvane datoteke
        var attachment = new MaterialAttachment(
            Guid.NewGuid(), _material.Id, fileName, "application/pdf", 7, key);
        await _attachments.AddAsync(attachment);
        return attachment;
    }

    private async Task<IReadOnlyList<Guid>> StudentSeesAsync() =>
        (await StudentListHandler().HandleAsync(
            new ListPublishedMaterialsForStudentQuery(StudentId, OfferingId)))
        .Select(m => m.Id)
        .ToArray();

    // Pozitivno ponašanje: ponovna objava

    [Test]
    public async Task Publish_WithdrawnMaterial_IsPublishedAgainAndSaved()
    {
        await PublishAndWithdrawAsync();

        var result = await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));

        var saved = await _materials.GetByIdAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
            Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
        });
    }

    [Test]
    public async Task Republished_MaterialIsVisibleToStudentAgain()
    {
        await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));
        var beforeWithdraw = await StudentSeesAsync();
        await WithdrawHandler().HandleAsync(new WithdrawMaterialCommand(TeacherId, _material.Id));
        var afterWithdraw = await StudentSeesAsync();

        await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));
        var afterRepublish = await StudentSeesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(beforeWithdraw, Is.EqualTo(new[] { _material.Id }));
            Assert.That(afterWithdraw, Is.Empty);
            Assert.That(afterRepublish, Is.EqualTo(new[] { _material.Id }));
        });
    }

    [Test]
    public async Task Republish_WithAllAttachmentFilesPresent_Succeeds()
    {
        await AddAttachmentAsync("skripta.pdf", withFile: true);
        await PublishAndWithdrawAsync();

        var result = await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
    }

    // Pozitivno ponašanje: povučen materijal ostaje vidljiv nastavniku

    [Test]
    public async Task Withdrawn_MaterialStaysInOfferingListWithAttachments()
    {
        var attachment = await AddAttachmentAsync("skripta.pdf", withFile: true);
        await PublishAndWithdrawAsync();

        var offeringMaterials = await _materials.ListByOfferingAsync(OfferingId);
        var attachments = await _attachments.ListByMaterialAsync(_material.Id);

        Assert.Multiple(() =>
        {
            Assert.That(offeringMaterials.Select(m => m.Id), Does.Contain(_material.Id));
            Assert.That(offeringMaterials.Single(m => m.Id == _material.Id).PublicationState,
                Is.EqualTo(MaterialPublicationState.Withdrawn));
            Assert.That(attachments.Select(a => a.Id), Is.EqualTo(new[] { attachment.Id }));
        });
    }

    [Test]
    public async Task Withdrawn_MaterialCanStillBeEditedByTeacher()
    {
        await PublishAndWithdrawAsync();
        var handler = new UpdateMaterialDetailsHandler(_materials, _staff, TimeProvider.System);

        var updated = await handler.HandleAsync(
            new UpdateMaterialDetailsCommand(TeacherId, _material.Id, "Predavanje 1 (ispravljeno)", "Novi opis"));

        var saved = await _materials.GetByIdAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(saved!.Name, Is.EqualTo("Predavanje 1 (ispravljeno)"));
            Assert.That(saved.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
        });
    }

    // Negativno ponašanje

    [Test]
    public async Task Republish_WhenAttachmentFileIsMissing_IsRejectedAndStaysWithdrawn()
    {
        await AddAttachmentAsync("nestala.pdf", withFile: false);
        await PublishAndWithdrawAsyncIgnoringAttachments();

        var result = await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));

        var saved = await _materials.GetByIdAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.MissingAttachmentFiles));
            Assert.That(result.MissingFileNames, Is.EqualTo(new[] { "nestala.pdf" }));
            Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
        });
    }

    [Test]
    public async Task Republish_ByUserWhoIsNotStaff_IsDeniedAndStaysWithdrawn()
    {
        await PublishAndWithdrawAsync();

        Assert.That(
            () => PublishHandler().HandleAsync(new PublishMaterialCommand(OtherUserId, _material.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());

        var saved = await _materials.GetByIdAsync(_material.Id);
        Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
    }

    // Granično ponašanje

    [Test]
    public async Task Publish_AlreadyPublished_StillReturnsNotDraft()
    {
        await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));

        var result = await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));

        Assert.That(result.Outcome, Is.EqualTo(PublishMaterialOutcome.NotDraft));
    }

    [Test]
    public async Task PublishWithdrawPublish_WorksRepeatedly()
    {
        for (var i = 0; i < 3; i++)
        {
            var published = await PublishHandler().HandleAsync(new PublishMaterialCommand(TeacherId, _material.Id));
            var withdrawn = await WithdrawHandler().HandleAsync(new WithdrawMaterialCommand(TeacherId, _material.Id));

            Assert.That(published.Outcome, Is.EqualTo(PublishMaterialOutcome.Published));
            Assert.That(withdrawn, Is.EqualTo(WithdrawMaterialOutcome.Withdrawn));
        }
    }

    // Materijal se objavljuje i povlači direktno u domenu, da bi prilog bez datoteke
    // postojao tek u trenutku ponovne objave (prva objava bi ga inače odbila).
    private async Task PublishAndWithdrawAsyncIgnoringAttachments()
    {
        _material.Publish();
        _material.Withdraw();
        await _materials.UpdateAsync(_material);
    }
}
