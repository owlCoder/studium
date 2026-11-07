using Application.Academic;
using Domain.Academic;
using Infrastructure.Academic;
using Infrastructure.Demo;
using NUnit.Framework;

namespace Tests.Academic;

[TestFixture]
public class CourseOfferingTests
{
    private static readonly Guid ErsId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static (CreateAcademicYearHandler yearHandler,
                    CreateCourseOfferingHandler offeringHandler,
                    InMemoryCourseOfferingRepository offerings)
        CreateHandlers()
    {
        var years = new InMemoryAcademicYearRepository();
        var offerings = new InMemoryCourseOfferingRepository();
        var courses = DemoData.CreateCourseReader();

        return (new CreateAcademicYearHandler(years),
                new CreateCourseOfferingHandler(offerings, years, courses),
                offerings);
    }

    private static CreateAcademicYearCommand Year(string label) =>
        new(label, new DateTime(2026, 10, 1), new DateTime(2027, 9, 30));

    [Test]
    public async Task CreateOffering_ValidData_Succeeds()
    {
        var (yearHandler, offeringHandler, offerings) = CreateHandlers();
        var yearId = await yearHandler.HandleAsync(Year("2026/2027"));

        var id = await offeringHandler.HandleAsync(new CreateCourseOfferingCommand(ErsId, yearId));

        var saved = await offerings.GetByIdAsync(id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.State, Is.EqualTo(OfferingState.InPreparation));
    }

    [Test]
    public async Task CreateOffering_SameCourseSameYear_IsRejected()
    {
        var (yearHandler, offeringHandler, _) = CreateHandlers();
        var yearId = await yearHandler.HandleAsync(Year("2026/2027"));
        await offeringHandler.HandleAsync(new CreateCourseOfferingCommand(ErsId, yearId));

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await offeringHandler.HandleAsync(new CreateCourseOfferingCommand(ErsId, yearId)));
    }

    [Test]
    public async Task CreateOffering_SameCourseDifferentYear_Succeeds()
    {
        var (yearHandler, offeringHandler, _) = CreateHandlers();
        var y1 = await yearHandler.HandleAsync(Year("2026/2027"));
        var y2 = await yearHandler.HandleAsync(Year("2027/2028"));

        await offeringHandler.HandleAsync(new CreateCourseOfferingCommand(ErsId, y1));
        var second = await offeringHandler.HandleAsync(new CreateCourseOfferingCommand(ErsId, y2));

        Assert.That(second, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task CreateAcademicYear_DuplicateLabel_IsRejected()
    {
        var (yearHandler, _, _) = CreateHandlers();
        await yearHandler.HandleAsync(Year("2026/2027"));

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await yearHandler.HandleAsync(Year("2026/2027")));
    }

    [Test]
    public void AcademicYear_StartAfterEnd_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new AcademicYear(Guid.NewGuid(), "2026/2027",
                new DateTime(2027, 9, 30), new DateTime(2026, 10, 1)));
    }

    [Test]
    public void ChangeState_ForwardOnly()
    {
        var offering = new CourseOffering(Guid.NewGuid(), ErsId, Guid.NewGuid());

        offering.ChangeState(OfferingState.Active);
        offering.ChangeState(OfferingState.Completed);

        Assert.That(offering.State, Is.EqualTo(OfferingState.Completed));
        Assert.Throws<InvalidOperationException>(() =>
            offering.ChangeState(OfferingState.Active));
    }

    [Test]
    public void ChangeState_SkippingActive_IsRejected()
    {
        var offering = new CourseOffering(Guid.NewGuid(), ErsId, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            offering.ChangeState(OfferingState.Completed));
    }
}
