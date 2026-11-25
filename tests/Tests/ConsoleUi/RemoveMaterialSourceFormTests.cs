using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-07 (tačka 3): akcija „Ukloni“ sa potvrdom za prilog ili spoljnu vezu.
/// </summary>
[TestFixture]
public sealed class RemoveMaterialSourceFormTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private string _root = null!;
    private LocalDiskAttachmentFileStorage _storage = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private InMemoryMaterialExternalLinkRepository _links = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us07-form-" + Guid.NewGuid().ToString("N"));
        _storage = new LocalDiskAttachmentFileStorage(_root);
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _links = new InMemoryMaterialExternalLinkRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        await _materials.AddAsync(_material);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private async Task<MaterialAttachment> AddAttachmentAsync(string name, int size = 7)
    {
        var key = await _storage.SaveAsync(new MemoryStream(new byte[size]));
        var attachment = new MaterialAttachment(Guid.NewGuid(), _material.Id, name, "application/pdf", size, key);
        await _attachments.AddAsync(attachment);
        return attachment;
    }

    private async Task<MaterialExternalLink> AddLinkAsync(string url)
    {
        var link = new MaterialExternalLink(Guid.NewGuid(), _material.Id, url);
        await _links.AddAsync(link);
        return link;
    }

    private async Task<(bool Removed, string Output)> RunFormAsync(
        string userInput, Guid? userId = null, Guid? materialId = null)
    {
        var staff = new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]);
        var output = new StringWriter();
        var form = new RemoveMaterialSourceForm(
            _materials,
            _attachments,
            _links,
            new RemoveMaterialAttachmentHandler(_materials, _attachments, _storage, staff),
            new RemoveMaterialExternalLinkHandler(_materials, _links, staff),
            new StringReader(userInput),
            output);

        var removed = await form.RunAsync(userId ?? TeacherId, materialId ?? _material.Id);
        return (removed, output.ToString());
    }

    // Pozitivno ponašanje

    [Test]
    public async Task RunAsync_ShowsNumberedAttachmentsThenLinks()
    {
        await AddLinkAsync("https://example.org/video");
        await AddAttachmentAsync("b-vezbe.pdf");
        await AddAttachmentAsync("a-uvod.pdf");

        var (_, output) = await RunFormAsync("\n");

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Prilozi i veze (3):"));
            Assert.That(output, Does.Contain("1. [prilog] a-uvod.pdf (application/pdf, 7 B)"));
            Assert.That(output, Does.Contain("2. [prilog] b-vezbe.pdf (application/pdf, 7 B)"));
            Assert.That(output, Does.Contain("3. [veza] https://example.org/video"));
        });
    }

    [Test]
    public async Task RunAsync_ConfirmedAttachment_IsRemovedWithItsFile()
    {
        var attachment = await AddAttachmentAsync("skripta.pdf");

        var (removed, output) = await RunFormAsync("1\nd\n");

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(output, Does.Contain("Ukloniti prilog 'skripta.pdf'? Datoteka se trajno briše. (d/N):"));
            Assert.That(output, Does.Contain("Prilog 'skripta.pdf' je uklonjen."));
            Assert.That(output, Does.Contain("Materijal više nema priloga ni spoljnih veza."));
        });
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Null);
        Assert.That(Directory.GetFiles(_root), Is.Empty);
    }

    [Test]
    public async Task RunAsync_ConfirmedLink_IsRemovedAndAttachmentStays()
    {
        var attachment = await AddAttachmentAsync("skripta.pdf");
        var link = await AddLinkAsync("https://example.org/video");

        var (removed, output) = await RunFormAsync("2\nda\n");

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(output, Does.Contain("Veza 'https://example.org/video' je uklonjena."));
            Assert.That(output, Does.Contain("Prilozi i veze (1):"));
        });
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Null);
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Not.Null);
    }

    [TestCase("D")]
    [TestCase(" da ")]
    [TestCase("DA")]
    public async Task RunAsync_ConfirmationIgnoresCaseAndSpaces(string answer)
    {
        var link = await AddLinkAsync("https://example.org/video");

        var (removed, _) = await RunFormAsync($"1\n{answer}\n");

        Assert.That(removed, Is.True);
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Null);
    }

    // Granično ponašanje: podrazumevano „ne“

    [TestCase("\n")]
    [TestCase("n\n")]
    [TestCase("ne\n")]
    [TestCase("možda\n")]
    [TestCase("")]
    public async Task RunAsync_NotConfirmed_NothingIsRemoved(string confirmation)
    {
        var attachment = await AddAttachmentAsync("skripta.pdf");

        var (removed, output) = await RunFormAsync("1\n" + confirmation);

        Assert.That(removed, Is.False);
        Assert.That(output, Does.Contain("Uklanjanje je otkazano. Ništa nije uklonjeno."));
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Not.Null);
        Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
    }

    [TestCase("\n")]
    [TestCase("")]
    public async Task RunAsync_NothingSelected_NothingIsRemoved(string selection)
    {
        var link = await AddLinkAsync("https://example.org/video");

        var (removed, output) = await RunFormAsync(selection);

        Assert.That(removed, Is.False);
        Assert.That(output, Does.Contain("Ništa nije izabrano. Ništa nije uklonjeno."));
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Not.Null);
    }

    [TestCase("0")]
    [TestCase("3")]
    [TestCase("abc")]
    [TestCase("-1")]
    public async Task RunAsync_InvalidNumber_AsksAgain(string invalid)
    {
        await AddAttachmentAsync("a.pdf");
        var link = await AddLinkAsync("https://example.org/video");

        var (removed, output) = await RunFormAsync($"{invalid}\n2\nd\n");

        Assert.That(output, Does.Contain("Unesite broj od 1 do 2."));
        Assert.That(removed, Is.True);
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Null);
    }

    [Test]
    public async Task RunAsync_MaterialWithoutSources_SaysSoAndAsksNothing()
    {
        var (removed, output) = await RunFormAsync("1\nd\n");

        Assert.That(removed, Is.False);
        Assert.That(output, Does.Contain("Materijal nema priloga ni spoljnih veza."));
        Assert.That(output, Does.Not.Contain("Broj za uklanjanje"));
    }

    // Negativno ponašanje

    [Test]
    public async Task RunAsync_UserNotStaff_IsDeniedAndNothingIsRemoved()
    {
        var attachment = await AddAttachmentAsync("skripta.pdf");

        var (removed, output) = await RunFormAsync("1\nd\n", userId: OtherUserId);

        Assert.That(removed, Is.False);
        Assert.That(output, Does.Contain("Ništa nije uklonjeno: Samo nastavno osoblje izvođenja može uklanjati priloge materijala."));
        Assert.That(await _attachments.GetByIdAsync(attachment.Id), Is.Not.Null);
        Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task RunAsync_UnknownMaterial_ReportsAndAsksNothing()
    {
        var (removed, output) = await RunFormAsync("1\nd\n", materialId: Guid.NewGuid());

        Assert.That(removed, Is.False);
        Assert.That(output, Does.Contain("Materijal nije pronađen."));
        Assert.That(output, Does.Not.Contain("Broj za uklanjanje"));
    }

    [Test]
    public async Task RunAsync_AttachmentRemovedMeanwhile_ReportsItNoLongerExists()
    {
        var attachment = await AddAttachmentAsync("skripta.pdf");
        var staff = new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]);
        var removeAttachment = new RemoveMaterialAttachmentHandler(_materials, _attachments, _storage, staff);
        var output = new StringWriter();
        // Drugi nastavnik ukloni prilog dok prvi čita pitanje za potvrdu.
        var input = new CallbackReader(["1", "d"], afterLine: 1, async () =>
            await removeAttachment.HandleAsync(new RemoveMaterialAttachmentCommand(TeacherId, _material.Id, attachment.Id)));
        var form = new RemoveMaterialSourceForm(
            _materials, _attachments, _links, removeAttachment,
            new RemoveMaterialExternalLinkHandler(_materials, _links, staff), input, output);

        var removed = await form.RunAsync(TeacherId, _material.Id);

        Assert.That(removed, Is.False);
        Assert.That(output.ToString(), Does.Contain("Prilog 'skripta.pdf' više ne postoji; možda je već uklonjen."));
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var staff = new InMemoryOfferingStaffChecker();
        var ra = new RemoveMaterialAttachmentHandler(_materials, _attachments, _storage, staff);
        var rl = new RemoveMaterialExternalLinkHandler(_materials, _links, staff);

        Assert.Multiple(() =>
        {
            Assert.That(() => new RemoveMaterialSourceForm(null!, _attachments, _links, ra, rl, TextReader.Null, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialSourceForm(_materials, null!, _links, ra, rl, TextReader.Null, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialSourceForm(_materials, _attachments, null!, ra, rl, TextReader.Null, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialSourceForm(_materials, _attachments, _links, null!, rl, TextReader.Null, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialSourceForm(_materials, _attachments, _links, ra, null!, TextReader.Null, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialSourceForm(_materials, _attachments, _links, ra, rl, null!, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialSourceForm(_materials, _attachments, _links, ra, rl, TextReader.Null, null!), Throws.TypeOf<ArgumentNullException>());
        });
    }

    /// <summary>Ulaz koji posle zadatog reda izvrši radnju (simulacija istovremenog korisnika).</summary>
    private sealed class CallbackReader(string[] lines, int afterLine, Func<Task> callback) : TextReader
    {
        private int _index;

        public override string? ReadLine()
        {
            if (_index == afterLine)
                callback().GetAwaiter().GetResult();
            return _index < lines.Length ? lines[_index++] : null;
        }
    }
}
