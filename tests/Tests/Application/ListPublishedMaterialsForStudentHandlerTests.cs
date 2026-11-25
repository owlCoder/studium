using Application.Access;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-10, tačka 1: student vidi samo objavljene materijale izvođenja na koje je upisan (ST-006).
/// </summary>
[TestFixture]
public sealed class ListPublishedMaterialsForStudentHandlerTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid OtherOfferingId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid EndedStudentId = Guid.NewGuid();
    private static readonly Guid OtherOfferingStudentId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;

    [SetUp]
    public void SetUp()
    {
        _materials = new InMemoryMaterialRepository();
    }

    private static InMemoryMembershipReader Memberships() =>
        new(
        [
            new OfferingMembership(StudentId, OfferingId, MembershipKind.Student, true),
            new OfferingMembership(EndedStudentId, OfferingId, MembershipKind.Student, false),
            new OfferingMembership(OtherOfferingStudentId, OtherOfferingId, MembershipKind.Student, true),
            new OfferingMembership(TeacherId, OfferingId, MembershipKind.Teacher, true)
        ]);

    private ListPublishedMaterialsForStudentHandler CreateHandler() => new(_materials, Memberships());

    private Task<IReadOnlyList<StudentMaterialListItem>> ListAsync(Guid userId, Guid? offeringId = null) =>
        CreateHandler().HandleAsync(new ListPublishedMaterialsForStudentQuery(userId, offeringId ?? OfferingId));

    private async Task<Material> AddAsync(string name, bool publish, Guid? offeringId = null, string? description = null)
    {
        var material = new Material(Guid.NewGuid(), offeringId ?? OfferingId, name, description);
        if (publish)
            material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_EnrolledStudent_GetsOnlyPublishedMaterials()
    {
        var published = await AddAsync("Predavanje 1", publish: true);
        await AddAsync("Nacrt predavanja 2", publish: false);

        var result = await ListAsync(StudentId);

        Assert.That(result.Select(m => m.Id), Is.EqualTo(new[] { published.Id }));
    }

    [Test]
    public async Task HandleAsync_ReturnsNameAndDescription()
    {
        var published = await AddAsync("Predavanje 1", publish: true, description: "Uvod u predmet");

        var result = await ListAsync(StudentId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(published.Id));
            Assert.That(result[0].Name, Is.EqualTo("Predavanje 1"));
            Assert.That(result[0].Description, Is.EqualTo("Uvod u predmet"));
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotReturnMaterialsOfAnotherOffering()
    {
        var own = await AddAsync("Predavanje 1", publish: true);
        await AddAsync("Tuđe predavanje", publish: true, offeringId: OtherOfferingId);

        var result = await ListAsync(StudentId);

        Assert.That(result.Select(m => m.Id), Is.EqualTo(new[] { own.Id }));
    }

    [Test]
    public async Task HandleAsync_ListIsOrderedByName()
    {
        await AddAsync("Vežbe 2", publish: true);
        await AddAsync("predavanje 1", publish: true);
        await AddAsync("Ispitna pitanja", publish: true);

        var result = await ListAsync(StudentId);

        Assert.That(
            result.Select(m => m.Name),
            Is.EqualTo(new[] { "Ispitna pitanja", "predavanje 1", "Vežbe 2" }));
    }

    [Test]
    public async Task HandleAsync_DoesNotChangeMaterials()
    {
        var draft = await AddAsync("Nacrt", publish: false);
        var published = await AddAsync("Objavljen", publish: true);

        await ListAsync(StudentId);

        Assert.Multiple(async () =>
        {
            Assert.That((await _materials.GetByIdAsync(draft.Id))!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
            Assert.That((await _materials.GetByIdAsync(published.Id))!.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
        });
    }

    // Negativno ponašanje

    [Test]
    public async Task HandleAsync_StudentOfAnotherOffering_IsDenied()
    {
        await AddAsync("Predavanje 1", publish: true);

        Assert.That(
            () => ListAsync(OtherOfferingStudentId),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public async Task HandleAsync_StudentWithEndedMembership_IsDenied()
    {
        await AddAsync("Predavanje 1", publish: true);

        Assert.That(
            () => ListAsync(EndedStudentId),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_UserWithoutAnyMembership_IsDenied()
    {
        Assert.That(
            () => ListAsync(Guid.NewGuid()),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_TeacherWithoutStudentMembership_IsDenied()
    {
        Assert.That(
            () => ListAsync(TeacherId),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_EmptyUser_IsDenied()
    {
        Assert.That(
            () => ListAsync(Guid.Empty),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_DeniedUser_MaterialsAreNotRead()
    {
        var materials = new Mock<IMaterialRepository>(MockBehavior.Strict);
        var handler = new ListPublishedMaterialsForStudentHandler(materials.Object, Memberships());

        Assert.That(
            () => handler.HandleAsync(new ListPublishedMaterialsForStudentQuery(OtherOfferingStudentId, OfferingId)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        materials.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_RejectsNullQuery()
    {
        Assert.That(() => CreateHandler().HandleAsync(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_RejectsMissingDependencies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new ListPublishedMaterialsForStudentHandler(null!, Memberships()),
                Throws.ArgumentNullException);
            Assert.That(
                () => new ListPublishedMaterialsForStudentHandler(_materials, null!),
                Throws.ArgumentNullException);
        });
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_NoMaterials_ReturnsEmptyList()
    {
        Assert.That(await ListAsync(StudentId), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_OnlyDrafts_ReturnsEmptyList()
    {
        await AddAsync("Nacrt 1", publish: false);
        await AddAsync("Nacrt 2", publish: false);

        Assert.That(await ListAsync(StudentId), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_WithdrawnMaterial_IsNotReturned()
    {
        var kept = await AddAsync("Predavanje 1", publish: true);
        var withdrawn = await AddAsync("Predavanje 2", publish: true);
        withdrawn.Withdraw();
        await _materials.UpdateAsync(withdrawn);

        var result = await ListAsync(StudentId);

        Assert.That(result.Select(m => m.Id), Is.EqualTo(new[] { kept.Id }));
    }

    [Test]
    public async Task HandleAsync_OnlyWithdrawnMaterials_ReturnsEmptyList()
    {
        var withdrawn = await AddAsync("Povučeno predavanje", publish: true);
        withdrawn.Withdraw();
        await _materials.UpdateAsync(withdrawn);

        Assert.That(await ListAsync(StudentId), Is.Empty);
    }

    [Test]
    public void HandleAsync_EmptyOffering_IsDenied()
    {
        Assert.That(
            () => ListAsync(StudentId, Guid.Empty),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public async Task HandleAsync_RepositoryReturnsForeignOrNullEntries_TheyAreIgnored()
    {
        var own = new Material(Guid.NewGuid(), OfferingId, "Svoj", null);
        own.Publish();
        var foreign = new Material(Guid.NewGuid(), OtherOfferingId, "Tuđ", null);
        foreign.Publish();

        var repository = new Mock<IMaterialRepository>();
        repository
            .Setup(r => r.ListByOfferingAsync(OfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { own, foreign, null! });
        var handler = new ListPublishedMaterialsForStudentHandler(repository.Object, Memberships());

        var result = await handler.HandleAsync(new ListPublishedMaterialsForStudentQuery(StudentId, OfferingId));

        Assert.That(result.Select(m => m.Id), Is.EqualTo(new[] { own.Id }));
    }
}
