using Domain.Academic;
using NUnit.Framework;

namespace Tests;

public class CourseOfferingStateTests
{
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
    public void ChangeState_Handler_NotImplementedYet()
    {
        Assert.Ignore("Čeka implementaciju ChangeOfferingStateHandler.");
    }
}
