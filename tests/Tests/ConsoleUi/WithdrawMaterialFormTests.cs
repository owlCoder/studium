using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-09 (tačka 3): dugme „Povuci“ sa potvrdom u konzoli.
/// </summary>
[TestFixture]
public sealed class WithdrawMaterialFormTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private InMemoryMaterialRepository _repository = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _repository = new InMemoryMaterialRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", "Uvod");
        _material.Publish();
        await _repository.AddAsync(_material);
    }

    private async Task<(bool Withdrawn, string Output)> RunFormAsync(
        string? userInput, Guid? materialId = null, Guid? userId = null)
    {
        var output = new StringWriter();
        var form = new WithdrawMaterialForm(
            _repository,
            new WithdrawMaterialHandler(
                _repository,
                new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)])),
            new StringReader(userInput ?? string.Empty),
            output);

        var withdrawn = await form.RunAsync(userId ?? TeacherId, materialId ?? _material.Id);
        return (withdrawn, output.ToString());
    }

    private async Task<MaterialPublicationState> StoredStateAsync() =>
        (await _repository.GetByIdAsync(_material.Id))!.PublicationState;

    // Pozitivno ponašanje

    [TestCase("d")]
    [TestCase("D")]
    [TestCase("da")]
    [TestCase(" Da ")]
    public async Task RunAsync_Confirmed_WithdrawsMaterial(string answer)
    {
        var (withdrawn, output) = await RunFormAsync(answer + Environment.NewLine);

        var state = await StoredStateAsync();
        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.True);
            Assert.That(state, Is.EqualTo(MaterialPublicationState.Withdrawn));
            Assert.That(output, Does.Contain("Materijal je povučen. Studenti ga više ne vide."));
        });
    }

    [Test]
    public async Task RunAsync_ShowsNameStateAndWarningBeforeAsking()
    {
        var (_, output) = await RunFormAsync("n" + Environment.NewLine);

        var prompt = output.IndexOf("(d/N)", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Naziv: Predavanje 1"));
            Assert.That(output, Does.Contain("Stanje: objavljen"));
            Assert.That(output, Does.Contain("Materijal se ne briše i može se ponovo objaviti."));
            Assert.That(prompt, Is.GreaterThan(output.IndexOf("Stanje: objavljen", StringComparison.Ordinal)));
        });
    }

    // Negativno ponašanje: potvrda nije data

    [TestCase("")]
    [TestCase("n")]
    [TestCase("ne")]
    [TestCase("x")]
    [TestCase("dd")]
    public async Task RunAsync_NotConfirmed_CancelsAndStateStaysPublished(string answer)
    {
        var (withdrawn, output) = await RunFormAsync(answer + Environment.NewLine);

        var state = await StoredStateAsync();
        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.False);
            Assert.That(state, Is.EqualTo(MaterialPublicationState.Published));
            Assert.That(output, Does.Contain("Povlačenje je otkazano. Materijal je i dalje objavljen."));
        });
    }

    [Test]
    public async Task RunAsync_InputEnds_CancelsAndStateStaysPublished()
    {
        var (withdrawn, output) = await RunFormAsync(null);

        var state = await StoredStateAsync();
        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.False);
            Assert.That(state, Is.EqualTo(MaterialPublicationState.Published));
            Assert.That(output, Does.Contain("Povlačenje je otkazano."));
        });
    }

    // Negativno ponašanje: pravo i stanje

    [Test]
    public async Task RunAsync_UserNotStaffConfirms_IsDeniedAndStateStaysPublished()
    {
        var (withdrawn, output) = await RunFormAsync("d" + Environment.NewLine, userId: OtherUserId);

        var state = await StoredStateAsync();
        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.False);
            Assert.That(state, Is.EqualTo(MaterialPublicationState.Published));
            Assert.That(output, Does.StartWith("Povlačenje materijala")
                .And.Contain("Materijal nije povučen: Samo nastavno osoblje izvođenja može povući materijal."));
        });
    }

    [Test]
    public async Task RunAsync_UnknownMaterial_ReportsNotFoundWithoutAsking()
    {
        var (withdrawn, output) = await RunFormAsync("d" + Environment.NewLine, materialId: Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.False);
            Assert.That(output, Does.Contain("Materijal nije pronađen."));
            Assert.That(output, Does.Not.Contain("(d/N)"));
        });
    }

    // Granično ponašanje

    [Test]
    public async Task RunAsync_Draft_IsNotOfferedForWithdrawal()
    {
        var draft = new Material(Guid.NewGuid(), _material.CourseOfferingId, "Nacrt", null);
        await _repository.AddAsync(draft);

        var (withdrawn, output) = await RunFormAsync("d" + Environment.NewLine, materialId: draft.Id);

        var stored = await _repository.GetByIdAsync(draft.Id);
        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.False);
            Assert.That(stored!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
            Assert.That(output, Does.Contain("Stanje: nacrt"));
            Assert.That(output, Does.Contain("Povući se može samo objavljen materijal."));
            Assert.That(output, Does.Not.Contain("(d/N)"));
        });
    }

    [Test]
    public async Task RunAsync_AlreadyWithdrawn_IsNotOfferedAgain()
    {
        await RunFormAsync("d" + Environment.NewLine);

        var (withdrawn, output) = await RunFormAsync("d" + Environment.NewLine);

        Assert.Multiple(() =>
        {
            Assert.That(withdrawn, Is.False);
            Assert.That(output, Does.Contain("Stanje: povučen"));
            Assert.That(output, Does.Not.Contain("(d/N)"));
        });
    }

    [Test]
    public void Constructor_NullDependencies_Throw()
    {
        var handler = new WithdrawMaterialHandler(_repository, new InMemoryOfferingStaffChecker());

        Assert.Multiple(() =>
        {
            Assert.That(() => new WithdrawMaterialForm(null!, handler, TextReader.Null, TextWriter.Null),
                Throws.ArgumentNullException);
            Assert.That(() => new WithdrawMaterialForm(_repository, null!, TextReader.Null, TextWriter.Null),
                Throws.ArgumentNullException);
            Assert.That(() => new WithdrawMaterialForm(_repository, handler, null!, TextWriter.Null),
                Throws.ArgumentNullException);
            Assert.That(() => new WithdrawMaterialForm(_repository, handler, TextReader.Null, null!),
                Throws.ArgumentNullException);
        });
    }
}
