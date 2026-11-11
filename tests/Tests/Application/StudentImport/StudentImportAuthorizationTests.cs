using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

[TestFixture]
public sealed class StudentImportAuthorizationTests
{
    private StudentImportFixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new StudentImportFixture();

    [Test]
    public void Student_Ne_Sme_Da_Uvozi()
    {
        var handler = _fixture.PreviewFor(_fixture.StudentId, AccountRoles.Student);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.HandleAsync(
                _fixture.OfferingId, StudentImportFixture.SpecExample));
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public void Asistent_Ne_Sme_Da_Uvozi()
    {
        var asistentId = Guid.NewGuid();
        _fixture.Assignments.Seed(asistentId, _fixture.OfferingId, TeachingResponsibility.Asistent);
        var handler = _fixture.ConfirmFor(asistentId, AccountRoles.Asistent);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.HandleAsync(
                _fixture.OfferingId, StudentImportFixture.SpecExample));
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public void Nastavnik_Angazovan_Ali_Ne_Odgovorni_Ne_Sme_Da_Uvozi()
    {
        _fixture.Assignments.Seed(_fixture.OtherTeacherId, _fixture.OfferingId, TeachingResponsibility.Nastavnik);
        var handler = _fixture.PreviewFor(_fixture.OtherTeacherId, AccountRoles.Nastavnik);

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.HandleAsync(
                _fixture.OfferingId, StudentImportFixture.SpecExample));

        Assert.That(ex!.Message, Does.Contain("odgovorni nastavnik"));
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public void Nastavnik_Bez_Angazovanja_Ne_Sme_Da_Uvozi()
    {
        var handler = _fixture.PreviewFor(_fixture.OtherTeacherId, AccountRoles.Nastavnik);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.HandleAsync(
                _fixture.OfferingId, StudentImportFixture.SpecExample));
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public async Task Odgovorni_Nastavnik_Dobija_Isti_Ishod_Kao_Administrator()
    {
        var asTeacher = await _fixture.PreviewFor(_fixture.TeacherId, AccountRoles.Nastavnik)
            .HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);
        var asAdmin = await _fixture.PreviewFor(_fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);

        Assert.That(asTeacher.CorrectCount, Is.EqualTo(17));
        Assert.That(asTeacher.Rows.Select(r => r.Status),
            Is.EqualTo(asAdmin.Rows.Select(r => r.Status)));
    }

    [Test]
    public void Neprijavljen_Korisnik_Ne_Sme_Da_Uvozi()
    {
        var handler = _fixture.PreviewFor(null, null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.HandleAsync(
                _fixture.OfferingId, StudentImportFixture.SpecExample));
    }

    [Test]
    public async Task Prava_Se_Proveravaju_I_Na_Potvrdi_Ne_Samo_Na_Pregledu()
    {
        await _fixture.PreviewFor(_fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);

        var confirmAsStudent = _fixture.ConfirmFor(_fixture.StudentId, AccountRoles.Student);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await confirmAsStudent.HandleAsync(
                _fixture.OfferingId, StudentImportFixture.SpecExample));
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Nepostojece_Izvodjenje_Se_Odbija(bool confirm)
    {
        var id = Guid.NewGuid();
        Assert.ThrowsAsync<ArgumentException>(async () => {
            if (confirm)
                await _fixture.ConfirmFor(_fixture.AdminId, AccountRoles.Administrator)
                    .HandleAsync(id, StudentImportFixture.SpecExample);
            else
                await _fixture.PreviewFor(_fixture.AdminId, AccountRoles.Administrator)
                    .HandleAsync(id, StudentImportFixture.SpecExample);
        });
        Assert.That(_fixture.Enrollments.Count, Is.Zero);
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public void Neovlasceni_Korisnik_Ne_Cita_Stream_Datoteke()
    {
        var spyReader = new SpyFileReader();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var preview = new PreviewStudentImportHandler(
            _fixture.AuthorizerFor(_fixture.StudentId, AccountRoles.Student),
            _fixture.OfferingGuard, _fixture.Parser, _fixture.Classifier, spyReader);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await preview.HandleAsync(_fixture.OfferingId, stream));

        var confirm = new ConfirmStudentImportHandler(
            _fixture.AuthorizerFor(_fixture.StudentId, AccountRoles.Student),
            _fixture.OfferingGuard, _fixture.Parser, _fixture.Classifier,
            _fixture.Processor, _fixture.UnitOfWork, _fixture.Clock, spyReader);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await confirm.HandleAsync(_fixture.OfferingId, stream));

        Assert.That(spyReader.Calls, Is.Zero);
    }

    private sealed class SpyFileReader : IStudentListFileReader
    {
        public int Calls { get; private set; }

        public Task<string> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(string.Empty);
        }
    }
}
