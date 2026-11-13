using Application.Academic;
using Domain.Academic;
using Infrastructure.Academic;
using NUnit.Framework;

namespace Tests.Academic;

[TestFixture]
public sealed class AcademicYearsHandlersTests
{
    private InMemoryAcademicYearRepository _repository = null!;
    private CreateAcademicYearHandler _create = null!;
    private ListAcademicYearsHandler _list = null!;
    private UpdateAcademicYearHandler _update = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryAcademicYearRepository();
        _create = new CreateAcademicYearHandler(_repository);
        _list = new ListAcademicYearsHandler(_repository);
        _update = new UpdateAcademicYearHandler(_repository);
    }

    private Task<Guid> CreateYear(string label, int startYear) =>
        _create.HandleAsync(new CreateAcademicYearCommand(
            label,
            new DateTime(startYear, 10, 1),
            new DateTime(startYear + 1, 9, 30)));

    // ---------- Lista i pretraga ----------

    [Test]
    public async Task List_ReturnsYearsSortedByStartDate()
    {
        await CreateYear("2027/2028", 2027);
        await CreateYear("2026/2027", 2026);

        var result = await _list.HandleAsync();

        Assert.That(result.Select(y => y.Label), Is.EqualTo(new[] { "2026/2027", "2027/2028" }));
    }

    [Test]
    public async Task Search_IsCaseInsensitive_AndTrimsQuery()
    {
        await CreateYear("2026/2027", 2026);
        await CreateYear("2027/2028", 2027);

        var result = await _list.HandleAsync("  2028 ");

        Assert.That(result.Select(y => y.Label), Is.EqualTo(new[] { "2027/2028" }));
    }

    [Test]
    public async Task Search_WithNoMatch_ReturnsEmptyList()
    {
        await CreateYear("2026/2027", 2026);

        var result = await _list.HandleAsync("1999");

        Assert.That(result, Is.Empty);
    }

    // ---------- Izmena: pozitivni scenariji ----------

    [Test]
    public async Task Update_ChangesValues_AndKeepsId()
    {
        var id = await CreateYear("2026/2027", 2026);

        await _update.HandleAsync(new UpdateAcademicYearCommand(
            id, "2026/2027", new DateTime(2026, 10, 1), new DateTime(2027, 10, 15)));

        var updated = await _repository.GetByIdAsync(id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Id, Is.EqualTo(id));
        Assert.That(updated.EndsAt, Is.EqualTo(new DateTime(2027, 10, 15)));
    }

    [Test]
    public async Task Update_KeepingOwnLabel_Succeeds()
    {
        var id = await CreateYear("2026/2027", 2026);

        Assert.DoesNotThrowAsync(() => _update.HandleAsync(new UpdateAcademicYearCommand(
            id, " 2026/2027 ", new DateTime(2026, 9, 1), new DateTime(2027, 9, 30))));
    }

    // ---------- Izmena: odbijeni scenariji ----------

    [Test]
    public void Update_NonexistentId_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _update.HandleAsync(
            new UpdateAcademicYearCommand(
                Guid.NewGuid(), "2099/2100", new DateTime(2099, 10, 1), new DateTime(2100, 9, 30))));
    }

    [Test]
    public async Task Update_StartAfterEnd_Throws_AndKeepsOldData()
    {
        var id = await CreateYear("2026/2027", 2026);

        Assert.ThrowsAsync<ArgumentException>(() => _update.HandleAsync(
            new UpdateAcademicYearCommand(
                id, "2026/2027", new DateTime(2027, 9, 30), new DateTime(2026, 10, 1))));

        var unchanged = await _repository.GetByIdAsync(id);
        Assert.That(unchanged!.StartsAt, Is.EqualTo(new DateTime(2026, 10, 1)));
        Assert.That(unchanged.EndsAt, Is.EqualTo(new DateTime(2027, 9, 30)));
    }

    [Test]
    public async Task Update_EmptyLabel_Throws_AndKeepsOldData()
    {
        var id = await CreateYear("2026/2027", 2026);

        Assert.ThrowsAsync<ArgumentException>(() => _update.HandleAsync(
            new UpdateAcademicYearCommand(
                id, "   ", new DateTime(2026, 10, 1), new DateTime(2027, 9, 30))));

        var unchanged = await _repository.GetByIdAsync(id);
        Assert.That(unchanged!.Label, Is.EqualTo("2026/2027"));
    }

    [Test]
    public async Task Update_LabelTakenByOtherYear_Throws_AndKeepsOldData()
    {
        await CreateYear("2026/2027", 2026);
        var secondId = await CreateYear("2027/2028", 2027);

        Assert.ThrowsAsync<InvalidOperationException>(() => _update.HandleAsync(
            new UpdateAcademicYearCommand(
                secondId, "2026/2027", new DateTime(2027, 10, 1), new DateTime(2028, 9, 30))));

        var unchanged = await _repository.GetByIdAsync(secondId);
        Assert.That(unchanged!.Label, Is.EqualTo("2027/2028"));
    }

    // ---------- Paralelne izmene ----------

    [Test]
    public async Task ConcurrentUpdates_ToSameLabel_OnlyOneSucceeds()
    {
        var idA = await CreateYear("2026/2027", 2026);
        var idB = await CreateYear("2027/2028", 2027);

        var a = new AcademicYear(idA, "2030/2031", new DateTime(2030, 10, 1), new DateTime(2031, 9, 30));
        var b = new AcademicYear(idB, "2030/2031", new DateTime(2030, 10, 1), new DateTime(2031, 9, 30));

        var results = await Task.WhenAll(
            Task.Run(() => _repository.TryUpdateAsync(a)),
            Task.Run(() => _repository.TryUpdateAsync(b)));

        Assert.That(results.Count(r => r), Is.EqualTo(1));
        var all = await _list.HandleAsync("2030/2031");
        Assert.That(all, Has.Count.EqualTo(1));
    }
}
