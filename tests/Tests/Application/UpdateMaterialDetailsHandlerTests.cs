using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-02: izmena naziva i opisa materijala i vreme poslednje izmene,
/// uz proveru da je korisnik nastavno osoblje izvođenja materijala (ST-005).
/// </summary>
[TestFixture]
public sealed class UpdateMaterialDetailsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    // Podrazumevano: TeacherId je nastavno osoblje na svakom izvođenju, drugi korisnici nisu.
    private static UpdateMaterialDetailsHandler CreateHandler(
        IMaterialRepository repository,
        TimeProvider? clock = null,
        IOfferingStaffChecker? staffChecker = null)
    {
        if (staffChecker is null)
        {
            var mock = new Mock<IOfferingStaffChecker>();
            mock.Setup(c => c.IsStaffOnOfferingAsync(TeacherId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            staffChecker = mock.Object;
        }

        return new(repository, staffChecker, clock ?? new FixedTimeProvider(Now));
    }

    [Test]
    public async Task HandleAsync_UpdatesExistingMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");
        await repository.AddAsync(material);
        var handler = CreateHandler(repository);

        var updated = await handler.HandleAsync(
            new UpdateMaterialDetailsCommand(TeacherId, material.Id, " Novo ", "Novi opis"));

        var stored = await repository.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(stored!.Name, Is.EqualTo("Novo"));
            Assert.That(stored.Description, Is.EqualTo("Novi opis"));
        });
    }

    [Test]
    public async Task HandleAsync_StoresLastModifiedTimeFromClock()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        await repository.AddAsync(material);
        var handler = CreateHandler(repository);

        await handler.HandleAsync(new UpdateMaterialDetailsCommand(TeacherId, material.Id, "Novo", null));

        var stored = await repository.GetByIdAsync(material.Id);
        Assert.That(stored!.LastModifiedAt, Is.EqualTo(Now));
    }

    [Test]
    public async Task HandleAsync_ReturnsFalseForUnknownMaterial()
    {
        var handler = CreateHandler(new InMemoryMaterialRepository());

        var updated = await handler.HandleAsync(
            new UpdateMaterialDetailsCommand(TeacherId, Guid.NewGuid(), "Novo", null));

        Assert.That(updated, Is.False);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task HandleAsync_InvalidNameDoesNotChangeStoredMaterial(string name)
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");
        await repository.AddAsync(material);
        var handler = CreateHandler(repository);

        Assert.That(
            async () => await handler.HandleAsync(
                new UpdateMaterialDetailsCommand(TeacherId, material.Id, name, "Novi opis")),
            Throws.TypeOf<ArgumentException>());

        var stored = await repository.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Name, Is.EqualTo("Staro"));
            Assert.That(stored.Description, Is.EqualTo("Stari opis"));
            Assert.That(stored.LastModifiedAt, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_ChangesOnlyRequestedMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var target = new Material(Guid.NewGuid(), Guid.NewGuid(), "Ciljni", null);
        var other = new Material(Guid.NewGuid(), Guid.NewGuid(), "Drugi", null);
        await repository.AddAsync(target);
        await repository.AddAsync(other);
        var handler = CreateHandler(repository);

        await handler.HandleAsync(new UpdateMaterialDetailsCommand(TeacherId, target.Id, "Izmenjen", null));

        var storedOther = await repository.GetByIdAsync(other.Id);
        Assert.Multiple(() =>
        {
            Assert.That(storedOther!.Name, Is.EqualTo("Drugi"));
            Assert.That(storedOther.LastModifiedAt, Is.Null);
        });
    }

    [Test]
    public void HandleAsync_RejectsNullCommand()
    {
        var handler = CreateHandler(new InMemoryMaterialRepository());

        Assert.That(
            async () => await handler.HandleAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Constructor_RejectsMissingDependencies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new UpdateMaterialDetailsHandler(null!, new InMemoryOfferingStaffChecker(), TimeProvider.System),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                () => new UpdateMaterialDetailsHandler(new InMemoryMaterialRepository(), null!, TimeProvider.System),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                () => new UpdateMaterialDetailsHandler(
                    new InMemoryMaterialRepository(), new InMemoryOfferingStaffChecker(), null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }

    // Provera nastavnog osoblja izvođenja (ST-005)

    [Test]
    public async Task HandleAsync_StaffOfMaterialOffering_CanUpdate()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        await repository.AddAsync(material);
        var handler = CreateHandler(
            repository,
            staffChecker: new InMemoryOfferingStaffChecker([(TeacherId, material.CourseOfferingId)]));

        var updated = await handler.HandleAsync(
            new UpdateMaterialDetailsCommand(TeacherId, material.Id, "Novo", null));

        Assert.That(updated, Is.True);
        Assert.That((await repository.GetByIdAsync(material.Id))!.Name, Is.EqualTo("Novo"));
    }

    [Test]
    public async Task HandleAsync_UserNotStaff_IsDeniedAndMaterialIsUnchanged()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");
        await repository.AddAsync(material);
        var handler = CreateHandler(repository);

        Assert.That(
            async () => await handler.HandleAsync(
                new UpdateMaterialDetailsCommand(OtherUserId, material.Id, "Novo", "Novi opis")),
            Throws.TypeOf<MaterialAccessDeniedException>());

        var stored = await repository.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Name, Is.EqualTo("Staro"));
            Assert.That(stored.Description, Is.EqualTo("Stari opis"));
            Assert.That(stored.LastModifiedAt, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_StaffOfAnotherOffering_IsDenied()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        await repository.AddAsync(material);
        var handler = CreateHandler(
            repository,
            staffChecker: new InMemoryOfferingStaffChecker([(TeacherId, Guid.NewGuid())]));

        Assert.That(
            async () => await handler.HandleAsync(
                new UpdateMaterialDetailsCommand(TeacherId, material.Id, "Novo", null)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That((await repository.GetByIdAsync(material.Id))!.Name, Is.EqualTo("Staro"));
    }

    [Test]
    public async Task HandleAsync_EmptyUserId_IsDenied()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        await repository.AddAsync(material);

        Assert.That(
            async () => await CreateHandler(repository).HandleAsync(
                new UpdateMaterialDetailsCommand(Guid.Empty, material.Id, "Novo", null)),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public async Task HandleAsync_UserNotStaffWithInvalidName_IsDeniedNotValidationError()
    {
        // Korisnik bez prava dobija odbijanje pristupa, a ne poruku o podacima.
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        await repository.AddAsync(material);

        Assert.That(
            async () => await CreateHandler(repository).HandleAsync(
                new UpdateMaterialDetailsCommand(OtherUserId, material.Id, "", null)),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
