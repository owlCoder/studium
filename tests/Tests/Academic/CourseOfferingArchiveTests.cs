using Application.Academic;
using Domain.Academic;
using Infrastructure.Academic;
using NUnit.Framework;

namespace Tests.Academic;

[TestFixture]
public class CourseOfferingArchiveTests
{
    private InMemoryCourseOfferingRepository _repository = null!;
    private ArchiveCourseOfferingHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCourseOfferingRepository();
        _handler = new ArchiveCourseOfferingHandler(_repository);
    }

    // Pravi izvođenje u traženom stanju i upisuje ga u repozitorijum
    private async Task<CourseOffering> AddOfferingInStateAsync(OfferingState state)
    {
        var offering = new CourseOffering(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        if (state is OfferingState.Active or OfferingState.Completed)
        {
            offering.ChangeState(OfferingState.Active);
        }

        if (state == OfferingState.Completed)
        {
            offering.ChangeState(OfferingState.Completed);
        }

        Assert.That(await _repository.TryAddAsync(offering), Is.True);
        return offering;
    }

    [Test]
    public async Task Archive_CompletedOffering_IsArchived()
    {
        var offering = await AddOfferingInStateAsync(OfferingState.Completed);

        await _handler.HandleAsync(new ArchiveCourseOfferingCommand(offering.Id));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.IsArchived, Is.True);
    }

    [Test]
    public async Task Archive_KeepsOfferingData()
    {
        var offering = await AddOfferingInStateAsync(OfferingState.Completed);

        await _handler.HandleAsync(new ArchiveCourseOfferingCommand(offering.Id));

        // Arhiviranje ne briše izvođenje niti menja njegove podatke
        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.CourseId, Is.EqualTo(offering.CourseId));
        Assert.That(stored.AcademicYearId, Is.EqualTo(offering.AcademicYearId));
        Assert.That(stored.State, Is.EqualTo(OfferingState.Completed));
    }

    [Test]
    public async Task Archive_InPreparationOffering_IsRejected()
    {
        var offering = await AddOfferingInStateAsync(OfferingState.InPreparation);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new ArchiveCourseOfferingCommand(offering.Id)));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.IsArchived, Is.False);
    }

    [Test]
    public async Task Archive_ActiveOffering_IsRejected()
    {
        var offering = await AddOfferingInStateAsync(OfferingState.Active);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new ArchiveCourseOfferingCommand(offering.Id)));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.IsArchived, Is.False);
    }

    [Test]
    public async Task Archive_AlreadyArchived_IsRejected()
    {
        var offering = await AddOfferingInStateAsync(OfferingState.Completed);
        await _handler.HandleAsync(new ArchiveCourseOfferingCommand(offering.Id));

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new ArchiveCourseOfferingCommand(offering.Id)));
    }

    [Test]
    public void Archive_NonExistingOffering_IsRejected()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new ArchiveCourseOfferingCommand(Guid.NewGuid())));
    }
}
