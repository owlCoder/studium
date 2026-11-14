using Application.Academic;
using Infrastructure.Academic;
using Infrastructure.Demo;
using NUnit.Framework;

namespace Tests.Academic;

[TestFixture]
public sealed class OfferingScenarioTests
{
    private CreateAcademicYearHandler _createYear = null!;
    private CreateCourseOfferingHandler _createOffering = null!;
    private Guid _courseId;

    [SetUp]
    public async Task SetUp()
    {
        var reader = DemoData.CreateCourseReader();
        var years = new InMemoryAcademicYearRepository();
        var offerings = new InMemoryCourseOfferingRepository();

        _createYear = new CreateAcademicYearHandler(years);
        _createOffering = new CreateCourseOfferingHandler(offerings, years, reader);
        _courseId = (await reader.ListAsync()).First(c => c.IsActive).Id;
    }

    private static CreateAcademicYearCommand ValidYear(string label = "2026/2027") =>
        new(label, new DateTime(2026, 10, 1), new DateTime(2027, 9, 30));

    [Test]
    public async Task CreateYear_ReturnsNewId()
    {
        var id = await _createYear.HandleAsync(ValidYear());

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task CreateYear_RejectsDuplicateLabel()
    {
        await _createYear.HandleAsync(ValidYear());

        Assert.CatchAsync<InvalidOperationException>(
            async () => await _createYear.HandleAsync(ValidYear()));
    }

    [Test]
    public void CreateYear_RejectsStartAfterEnd()
    {
        var command = new CreateAcademicYearCommand(
            "2030/2031", new DateTime(2031, 9, 30), new DateTime(2030, 10, 1));

        Assert.CatchAsync<ArgumentException>(
            async () => await _createYear.HandleAsync(command));
    }

    [Test]
    public async Task CreateOffering_ReturnsNewId()
    {
        var yearId = await _createYear.HandleAsync(ValidYear());

        var id = await _createOffering.HandleAsync(
            new CreateCourseOfferingCommand(_courseId, yearId));

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task CreateOffering_RejectsDuplicatePair()
    {
        var yearId = await _createYear.HandleAsync(ValidYear());
        var command = new CreateCourseOfferingCommand(_courseId, yearId);
        await _createOffering.HandleAsync(command);

        Assert.CatchAsync<InvalidOperationException>(
            async () => await _createOffering.HandleAsync(command));
    }

    [Test]
    public void CreateOffering_RejectsUnknownYear()
    {
        var command = new CreateCourseOfferingCommand(_courseId, Guid.NewGuid());

        Assert.CatchAsync<InvalidOperationException>(
            async () => await _createOffering.HandleAsync(command));
    }

    [Test]
    public async Task CreateOffering_RejectsUnknownCourse()
    {
        var yearId = await _createYear.HandleAsync(ValidYear());
        var command = new CreateCourseOfferingCommand(Guid.NewGuid(), yearId);

        Assert.CatchAsync<InvalidOperationException>(
            async () => await _createOffering.HandleAsync(command));
    }
}
