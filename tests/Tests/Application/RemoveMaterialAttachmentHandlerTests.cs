using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-07 (tačka 1): uklanjanje priloga iz materijala.
/// </summary>
[TestFixture]
public sealed class RemoveMaterialAttachmentHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        await _materials.AddAsync(_material);
    }

    private RemoveMaterialAttachmentHandler CreateHandler(IMaterialAttachmentRepository? attachments = null) =>
        new(
            _materials,
            attachments ?? _attachments,
            new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]));

    private async Task<MaterialAttachment> AddAttachmentAsync(Guid materialId, string name = "skripta.pdf")
    {
        var attachment = new MaterialAttachment(
            Guid.NewGuid(), materialId, name, "application/pdf", 10, Guid.NewGuid().ToString("N"));
        await _attachments.AddAsync(attachment);
        return attachment;
    }

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_TeacherRemovesAttachment_RecordIsRemoved()
    {
        var attachment = await AddAttachmentAsync(_material.Id);

        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, attachment.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialAttachmentOutcome.Removed));
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Null);
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_OtherAttachmentsOfMaterialStay()
    {
        var removed = await AddAttachmentAsync(_material.Id, "staro.pdf");
        var kept = await AddAttachmentAsync(_material.Id, "aktuelno.pdf");

        await CreateHandler().HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, removed.Id));

        var left = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.That(left.Select(a => a.Id), Is.EqualTo(new[] { kept.Id }));
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_RemovingSameAttachmentTwice_SecondTimeIsNotFound()
    {
        var attachment = await AddAttachmentAsync(_material.Id);
        var handler = CreateHandler();
        var command = new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, attachment.Id);

        var first = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);

        Assert.That(first, Is.EqualTo(RemoveMaterialAttachmentOutcome.Removed));
        Assert.That(second, Is.EqualTo(RemoveMaterialAttachmentOutcome.AttachmentNotFound));
    }

    [Test]
    public async Task HandleAsync_AttachmentRemovedByConcurrentRequest_IsNotFound()
    {
        var attachment = await AddAttachmentAsync(_material.Id);
        var repository = new Mock<IMaterialAttachmentRepository>();
        repository
            .Setup(r => r.GetByIdAsync(attachment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        repository
            .Setup(r => r.RemoveAsync(attachment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var outcome = await CreateHandler(repository.Object).HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, attachment.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialAttachmentOutcome.AttachmentNotFound));
    }

    [Test]
    public async Task HandleAsync_LastAttachment_MaterialStaysWithoutAttachments()
    {
        var attachment = await AddAttachmentAsync(_material.Id);

        await CreateHandler().HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, attachment.Id));

        Assert.That(await _materials.GetByIdAsync(_material.Id), Is.Not.Null);
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    // Negativno ponašanje

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsMaterialNotFoundAndRemovesNothing()
    {
        var attachment = await AddAttachmentAsync(_material.Id);

        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, Guid.NewGuid(), attachment.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialAttachmentOutcome.MaterialNotFound));
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_UnknownAttachment_ReturnsAttachmentNotFound()
    {
        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, Guid.NewGuid()));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialAttachmentOutcome.AttachmentNotFound));
    }

    [Test]
    public async Task HandleAsync_AttachmentOfAnotherMaterial_IsNotRemoved()
    {
        var otherMaterial = new Material(Guid.NewGuid(), _material.CourseOfferingId, "Predavanje 2", null);
        await _materials.AddAsync(otherMaterial);
        var foreign = await AddAttachmentAsync(otherMaterial.Id);

        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, foreign.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialAttachmentOutcome.AttachmentNotFound));
        Assert.That(await _attachments.GetByIdAsync(foreign.Id), Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_UserNotStaff_IsDeniedAndRemovesNothing()
    {
        var attachment = await AddAttachmentAsync(_material.Id);

        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialAttachmentCommand(OtherUserId, _material.Id, attachment.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_StaffOfAnotherOffering_IsDenied()
    {
        var otherMaterial = new Material(Guid.NewGuid(), Guid.NewGuid(), "Tudji materijal", null);
        await _materials.AddAsync(otherMaterial);
        var attachment = await AddAttachmentAsync(otherMaterial.Id);

        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialAttachmentCommand(TeacherId, otherMaterial.Id, attachment.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Not.Null);
    }

    [Test]
    public void HandleAsync_EmptyUserId_IsDenied()
    {
        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialAttachmentCommand(Guid.Empty, _material.Id, Guid.NewGuid())),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_UserNotStaffAndUnknownAttachment_IsDeniedNotNotFound()
    {
        // Korisnik bez prava ne sme da sazna da li prilog postoji.
        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialAttachmentCommand(OtherUserId, _material.Id, Guid.NewGuid())),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_NullCommand_Throws()
    {
        Assert.That(() => CreateHandler().HandleAsync(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var staff = new InMemoryOfferingStaffChecker();

        Assert.Multiple(() =>
        {
            Assert.That(() => new RemoveMaterialAttachmentHandler(null!, _attachments, staff),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialAttachmentHandler(_materials, null!, staff),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialAttachmentHandler(_materials, _attachments, null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
