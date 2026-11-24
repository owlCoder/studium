using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryMaterialAttachmentRepositoryTests
{
    private static MaterialAttachment CreateAttachment(Guid materialId, string name = "a.pdf") =>
        new(Guid.NewGuid(), materialId, name, "application/pdf", 100, Guid.NewGuid().ToString("N"));

    // Pozitivno ponašanje

    [Test]
    public async Task AddAsync_ThenListByMaterial_ReturnsAttachmentWithMetadata()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var materialId = Guid.NewGuid();
        var attachment = CreateAttachment(materialId, "Predavanje 1.pdf");

        await repository.AddAsync(attachment);
        var result = await repository.ListByMaterialAsync(materialId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OriginalFileName, Is.EqualTo("Predavanje 1.pdf"));
        Assert.That(result[0].ContentType, Is.EqualTo("application/pdf"));
        Assert.That(result[0].SizeInBytes, Is.EqualTo(100));
    }

    [Test]
    public async Task ListByMaterial_ReturnsOnlyAttachmentsOfThatMaterial()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var materialId = Guid.NewGuid();
        var otherMaterialId = Guid.NewGuid();
        var first = CreateAttachment(materialId, "1.pdf");
        var second = CreateAttachment(materialId, "2.pdf");
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await repository.AddAsync(CreateAttachment(otherMaterialId, "tudji.pdf"));

        var result = await repository.ListByMaterialAsync(materialId);

        Assert.That(result.Select(a => a.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public async Task AddAsync_SameOriginalNameTwice_StoresBothAttachments()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var materialId = Guid.NewGuid();
        await repository.AddAsync(CreateAttachment(materialId, "isti.pdf"));
        await repository.AddAsync(CreateAttachment(materialId, "isti.pdf"));

        var result = await repository.ListByMaterialAsync(materialId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    // Granično ponašanje

    [Test]
    public async Task ListByMaterial_WithoutAttachments_ReturnsEmptyList()
    {
        var repository = new InMemoryMaterialAttachmentRepository();

        var result = await repository.ListByMaterialAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    // Negativno ponašanje

    [Test]
    public async Task AddAsync_DuplicateId_ThrowsAndKeepsOriginal()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var materialId = Guid.NewGuid();
        var original = CreateAttachment(materialId, "original.pdf");
        await repository.AddAsync(original);
        var duplicate = new MaterialAttachment(
            original.Id, materialId, "duplikat.pdf", "application/pdf", 1, Guid.NewGuid().ToString("N"));

        Assert.That(
            () => repository.AddAsync(duplicate),
            Throws.InvalidOperationException);

        var result = await repository.ListByMaterialAsync(materialId);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OriginalFileName, Is.EqualTo("original.pdf"));
    }

    [Test]
    public void AddAsync_Null_Throws()
    {
        var repository = new InMemoryMaterialAttachmentRepository();

        Assert.That(
            () => repository.AddAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public async Task AddAsync_WhenCancelled_ThrowsAndStoresNothing()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var materialId = Guid.NewGuid();
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => repository.AddAsync(CreateAttachment(materialId), source.Token),
            Throws.InstanceOf<OperationCanceledException>());

        var result = await repository.ListByMaterialAsync(materialId);
        Assert.That(result, Is.Empty);
    }

    // US-07: čitanje i uklanjanje jednog priloga

    [Test]
    public async Task GetByIdAsync_ReturnsSavedAttachment()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var attachment = CreateAttachment(Guid.NewGuid(), "a.pdf");
        await repository.AddAsync(attachment);

        var result = await repository.GetByIdAsync(attachment.Id);

        Assert.That(result, Is.SameAs(attachment));
    }

    [Test]
    public async Task GetByIdAsync_Unknown_ReturnsNull()
    {
        var repository = new InMemoryMaterialAttachmentRepository();

        Assert.That(await repository.GetByIdAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task RemoveAsync_RemovesOnlyThatAttachment()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var materialId = Guid.NewGuid();
        var removed = CreateAttachment(materialId, "staro.pdf");
        var kept = CreateAttachment(materialId, "aktuelno.pdf");
        await repository.AddAsync(removed);
        await repository.AddAsync(kept);

        var result = await repository.RemoveAsync(removed.Id);

        Assert.That(result, Is.True);
        Assert.That((await repository.ListByMaterialAsync(materialId)).Select(a => a.Id),
            Is.EqualTo(new[] { kept.Id }));
    }

    [Test]
    public async Task RemoveAsync_UnknownOrAlreadyRemoved_ReturnsFalse()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var attachment = CreateAttachment(Guid.NewGuid());
        await repository.AddAsync(attachment);
        await repository.RemoveAsync(attachment.Id);

        Assert.That(await repository.RemoveAsync(attachment.Id), Is.False);
        Assert.That(await repository.RemoveAsync(Guid.NewGuid()), Is.False);
    }

    [Test]
    public async Task RemoveAsync_WhenCancelled_ThrowsAndKeepsAttachment()
    {
        var repository = new InMemoryMaterialAttachmentRepository();
        var attachment = CreateAttachment(Guid.NewGuid());
        await repository.AddAsync(attachment);
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => repository.RemoveAsync(attachment.Id, source.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(await repository.GetByIdAsync(attachment.Id), Is.Not.Null);
    }
}
