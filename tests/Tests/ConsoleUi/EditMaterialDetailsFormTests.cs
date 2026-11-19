using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-02: konzolna forma za izmenu sa popunjenim postojećim podacima.
/// </summary>
[TestFixture]
public sealed class EditMaterialDetailsFormTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    private InMemoryMaterialRepository _repository = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _repository = new InMemoryMaterialRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", "Uvod");
        await _repository.AddAsync(_material);
    }

    private async Task<(bool Saved, string Output)> RunFormAsync(string userInput, Guid? materialId = null)
    {
        var output = new StringWriter();
        var form = new EditMaterialDetailsForm(
            _repository,
            new UpdateMaterialDetailsHandler(_repository, new FixedTimeProvider(Now)),
            new StringReader(userInput),
            output);

        var saved = await form.RunAsync(materialId ?? _material.Id);
        return (saved, output.ToString());
    }

    [Test]
    public async Task RunAsync_ShowsExistingValuesBeforeInput()
    {
        var (_, output) = await RunFormAsync("\n\n");

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Naziv: Predavanje 1"));
            Assert.That(output, Does.Contain("Opis: Uvod"));
            Assert.That(output, Does.Contain("Novi naziv [Predavanje 1]"));
            Assert.That(output, Does.Contain("Poslednja izmena: nije menjan"));
        });
    }

    [Test]
    public async Task RunAsync_SavesNewNameAndDescription()
    {
        var (saved, output) = await RunFormAsync("Predavanje 2\nNovi opis\n");

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(_material.Name, Is.EqualTo("Predavanje 2"));
            Assert.That(_material.Description, Is.EqualTo("Novi opis"));
            Assert.That(_material.LastModifiedAt, Is.EqualTo(Now));
            Assert.That(output, Does.Contain("Izmena je sačuvana."));
            Assert.That(output, Does.Contain("Poslednja izmena: 10.11.2026. 10:00:00 UTC"));
        });
    }

    [Test]
    public async Task RunAsync_EmptyNameKeepsExistingName()
    {
        var (saved, _) = await RunFormAsync("\nNovi opis\n");

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(_material.Name, Is.EqualTo("Predavanje 1"));
            Assert.That(_material.Description, Is.EqualTo("Novi opis"));
        });
    }

    [Test]
    public async Task RunAsync_ClearMarkerRemovesDescription()
    {
        var (saved, _) = await RunFormAsync($"\n{EditMaterialDetailsForm.ClearDescriptionMarker}\n");

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(_material.Description, Is.Empty);
        });
    }

    [TestCase("\n\n")]
    [TestCase("   \n   \n")]
    [TestCase("Predavanje 1\nUvod\n")]
    [TestCase("")]
    public async Task RunAsync_NoChangesDoesNotSaveOrSetModifiedTime(string userInput)
    {
        var (saved, output) = await RunFormAsync(userInput);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.False);
            Assert.That(output, Does.Contain("Nema izmena."));
            Assert.That(_material.LastModifiedAt, Is.Null);
        });
    }

    [Test]
    public async Task RunAsync_UnknownMaterialReportsNotFound()
    {
        var (saved, output) = await RunFormAsync("Novo\n\n", Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.False);
            Assert.That(output, Does.Contain("Materijal nije pronađen."));
        });
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
