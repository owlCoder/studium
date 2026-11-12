using Application.Activities;
using Domain.Activity;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class CreateActivityHandlerTests
{
    private static readonly Guid ContextId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ContextSlug = "ERS-11111";
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    private static CreateActivityCommand Command(
        string contextSlug = ContextSlug,
        string name = "Timski projekat") =>
        new(contextSlug, name, "Prva faza", Start, End);

    private static Mock<IActivityContextReader> ContextReaderReturning(ActivityContextLookup lookup)
    {
        var reader = new Mock<IActivityContextReader>();
        reader.Setup(x => x.ResolveAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookup);
        return reader;
    }

    private static Mock<IActivityContextReader> ExistingContext() =>
        ContextReaderReturning(ActivityContextLookup.Found(new ActivityContext(ContextId)));

    private static Mock<IActivityRepository> RepositoryReturning(ActivityAddResult outcome)
    {
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);
        return repository;
    }

    [Test]
    public async Task HandleAsync_CreatesActivityForResolvedContext()
    {
        Activity? stored = null;
        var repository = new Mock<IActivityRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Callback<Activity?, CancellationToken>((activity, _) => stored = activity)
            .ReturnsAsync(ActivityAddResult.Added());

        var handler = new CreateActivityHandler(repository.Object, ExistingContext().Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Created));
            Assert.That(result.Activity, Is.Not.Null);
            Assert.That(result.Activity!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Activity.ContextId, Is.EqualTo(ContextId));
            Assert.That(result.Activity.Name, Is.EqualTo("Timski projekat"));
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Id, Is.EqualTo(result.Activity.Id));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_ReturnsContextNotFoundAndDoesNotStore()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(
            repository.Object,
            ContextReaderReturning(ActivityContextLookup.NotFound(
                $"Kontekst \"{ContextSlug}\" nije pronađen.")).Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.ContextNotFound));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Does.Contain(ContextSlug));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenDomainValidationFails()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(repository.Object, ExistingContext().Object);

        var result = await handler.HandleAsync(Command(name: " "));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenIntervalIsReversed()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(repository.Object, ExistingContext().Object);

        var result = await handler.HandleAsync(new CreateActivityCommand(
            ContextSlug, "Aktivnost", "Opis", End, Start));

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsConflictWhenStoreReportsDuplicateId()
    {
        var repository = RepositoryReturning(ActivityAddResult.DuplicateId());
        var handler = new CreateActivityHandler(repository.Object, ExistingContext().Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Conflict));
            Assert.That(result.Activity, Is.Null);
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsCanceledWhenStoreCancelsTheOperation()
    {
        var repository = RepositoryReturning(ActivityAddResult.Canceled());
        var handler = new CreateActivityHandler(repository.Object, ExistingContext().Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Canceled));
            Assert.That(result.Activity, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenStoreRejectsActivity()
    {
        var repository = RepositoryReturning(ActivityAddResult.InvalidActivity());
        var handler = new CreateActivityHandler(repository.Object, ExistingContext().Object);

        var result = await handler.HandleAsync(Command());

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
    }

    [Test]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var repository = RepositoryReturning(ActivityAddResult.Added());
        var reader = ExistingContext();
        var handler = new CreateActivityHandler(repository.Object, reader.Object);
        using var source = new CancellationTokenSource();

        await handler.HandleAsync(Command(), source.Token);

        reader.Verify(x => x.ResolveAsync(ContextSlug, source.Token), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<Activity>(), source.Token), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PassesSlugToContextReader()
    {
        var repository = RepositoryReturning(ActivityAddResult.Added());
        var reader = ExistingContext();
        var handler = new CreateActivityHandler(repository.Object, reader.Object);

        var result = await handler.HandleAsync(Command("ers-11111"));

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Created));
        reader.Verify(x => x.ResolveAsync("ers-11111", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ReturnsCanceledWhenContextResolutionIsCanceled()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(
            repository.Object,
            ContextReaderReturning(ActivityContextLookup.Canceled()).Object);

        var result = await handler.HandleAsync(Command());

        Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.Canceled));
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsInvalidRequestWhenContextIsAmbiguous()
    {
        var repository = new Mock<IActivityRepository>();
        var handler = new CreateActivityHandler(
            repository.Object,
            ContextReaderReturning(ActivityContextLookup.Ambiguous(
                $"Oznaka \"{ContextSlug}\" nije jedinstvena.")).Object);

        var result = await handler.HandleAsync(Command());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CreateActivityStatus.InvalidRequest));
            Assert.That(result.Error, Does.Contain(ContextSlug));
        });
        repository.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
