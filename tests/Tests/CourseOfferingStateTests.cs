using Application.Academic;
using Domain.Academic;
using Infrastructure.Academic;
using NUnit.Framework;

namespace Tests;

public class CourseOfferingStateTests
{
    private InMemoryCourseOfferingRepository _repository = null!;
    private ChangeOfferingStateHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCourseOfferingRepository();
        _handler = new ChangeOfferingStateHandler(_repository);
    }

    private async Task<CourseOffering> AddOfferingAsync()
    {
        var offering = new CourseOffering(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await _repository.AddAsync(offering);
        return offering;
    }

    [Test]
    public void ChangeState_InPreparationToActive_IsAllowed()
    {
        var offering = new CourseOffering(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        offering.ChangeState(OfferingState.Active);

        Assert.That(offering.State, Is.EqualTo(OfferingState.Active));
    }

    [Test]
    public void ChangeState_InPreparationToCompleted_IsRejected()
    {
        var offering = new CourseOffering(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(
            () => offering.ChangeState(OfferingState.Completed));
    }

    [Test]
    public async Task Handler_InPreparationToActive_ChangesStoredState()
    {
        var offering = await AddOfferingAsync();

        await _handler.HandleAsync(new ChangeOfferingStateCommand(offering.Id, OfferingState.Active));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.State, Is.EqualTo(OfferingState.Active));
    }

    [Test]
    public async Task Handler_ActiveToCompleted_ChangesStoredState()
    {
        var offering = await AddOfferingAsync();
        await _handler.HandleAsync(new ChangeOfferingStateCommand(offering.Id, OfferingState.Active));

        await _handler.HandleAsync(new ChangeOfferingStateCommand(offering.Id, OfferingState.Completed));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.State, Is.EqualTo(OfferingState.Completed));
    }

    [Test]
    public void Handler_NonExistingOffering_ThrowsAndChangesNothing()
    {
        var command = new ChangeOfferingStateCommand(Guid.NewGuid(), OfferingState.Active);

        Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
    }

    [Test]
    public async Task Handler_SkippingState_IsRejectedAndStateUnchanged()
    {
        var offering = await AddOfferingAsync();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new ChangeOfferingStateCommand(offering.Id, OfferingState.Completed)));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.State, Is.EqualTo(OfferingState.InPreparation));
    }

    [Test]
    public async Task Handler_GoingBackwards_IsRejected()
    {
        var offering = await AddOfferingAsync();
        await _handler.HandleAsync(new ChangeOfferingStateCommand(offering.Id, OfferingState.Active));

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new ChangeOfferingStateCommand(offering.Id, OfferingState.InPreparation)));

        var stored = await _repository.GetByIdAsync(offering.Id);
        Assert.That(stored!.State, Is.EqualTo(OfferingState.Active));
    }
}
