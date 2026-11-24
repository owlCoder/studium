using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Неуспех уписа или прве активације мора поништити све измене потврде.</summary>
[TestFixture]
public sealed class TransakcijaTests
{
    /// <summary>Тестни адаптер за чланства који баца изузетак на задатом позиву.</summary>
    private sealed class FailingEnrollmentDirectory : IEnrollmentDirectory
    {
        private readonly IEnrollmentDirectory _inner;
        private readonly int _failOnCall;
        private int _calls;

        public FailingEnrollmentDirectory(IEnrollmentDirectory inner, int failOnCall)
        {
            _inner = inner;
            _failOnCall = failOnCall;
        }

        public Task<EnrollmentSnapshot?> FindActiveAsync(
            Guid accountId, Guid offeringId, CancellationToken ct = default)
            => _inner.FindActiveAsync(accountId, offeringId, ct);

        public async Task<bool> TryAddActiveAsync(NewEnrollment enrollment, CancellationToken ct = default)
        {
            if (++_calls == _failOnCall)
                throw new InvalidOperationException("Prekid u skladistu.");

            return await _inner.TryAddActiveAsync(enrollment, ct);
        }
    }

    /// <summary>Замена која пријављује неуспех покретања активације.</summary>
    private sealed class FailingFirstActivationService : IFirstActivationService
    {
        private readonly IFirstActivationService _inner;
        private readonly int _failOnCall;
        private int _calls;

        public FailingFirstActivationService(IFirstActivationService inner, int failOnCall)
        {
            _inner = inner;
            _failOnCall = failOnCall;
        }

        public async Task StartAsync(Guid accountId, CancellationToken ct = default)
        {
            if (++_calls == _failOnCall)
                throw new InvalidOperationException("Prekid pri pokretanju aktivacije.");

            await _inner.StartAsync(accountId, ct);
        }
    }

    private const string ThreeRows =
        "firstname,lastname,username\n" +
        "Ana,Anić,ana.anic\n" +
        "Marko,Marković,marko.markovic\n" +
        "Jelena,Jelić,jelena.jelic\n";

    [Test]
    public async Task Prekid_Na_Drugom_Redu_Ne_Ostavlja_Nista()
    {
        var fixture = new StudentImportFixture();
        var failing = new FailingEnrollmentDirectory(fixture.Enrollments, failOnCall: 2);

        var processor = new ImportRowProcessor(fixture.Accounts, failing, fixture.Activations);
        var handler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard,
            fixture.Parser,
            fixture.Classifier,
            processor,
            fixture.UnitOfWork,
            fixture.Clock);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(fixture.OfferingId, ThreeRows));

        // Поништени су и налог, чланство и захтев за активацију из првог реда.
        Assert.That(fixture.Enrollments.Count, Is.Zero);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("marko.markovic"), Is.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("jelena.jelic"), Is.Null);
    }

    [Test]
    public async Task Prekid_Prve_Aktivacije_Vraca_I_Nalog_I_Clanstva()
    {
        var fixture = new StudentImportFixture();
        var failingActivation = new FailingFirstActivationService(fixture.Activations, failOnCall: 2);

        var processor = new ImportRowProcessor(fixture.Accounts, fixture.Enrollments, failingActivation);
        var handler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard,
            fixture.Parser,
            fixture.Classifier,
            processor,
            fixture.UnitOfWork,
            fixture.Clock);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(fixture.OfferingId, ThreeRows));

        Assert.That(fixture.Enrollments.Count, Is.Zero);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("marko.markovic"), Is.Null);
    }

    [Test]
    public async Task Uspesna_Potvrda_Zadrzava_Sve_Redove()
    {
        var fixture = new StudentImportFixture();

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, ThreeRows);

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(3));
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(3));
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(3));
    }

    /// <summary>Поништавање чува записе који су постојали пре потврде.</summary>
    [Test]
    public async Task Vracanje_Cuva_Postojece_Zapise()
    {
        var fixture = new StudentImportFixture();
        var existingId = Guid.NewGuid();
        fixture.Accounts.Seed("postoji.vec", existingId);
        fixture.Enrollments.Seed(existingId, fixture.OfferingId);
        var membershipBefore = fixture.Enrollments.All.Single();

        var processor = new ImportRowProcessor(
            fixture.Accounts,
            new FailingEnrollmentDirectory(fixture.Enrollments, failOnCall: 1),
            fixture.Activations);
        var handler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard, fixture.Parser, fixture.Classifier, processor,
            fixture.UnitOfWork, fixture.Clock);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(fixture.OfferingId, ThreeRows));

        Assert.That(fixture.Enrollments.Count, Is.EqualTo(1));
        var existing = await fixture.Accounts.FindByUsernameAsync("postoji.vec");
        Assert.That(existing, Is.Not.Null);
        Assert.That(existing!.AccountId, Is.EqualTo(existingId));
        Assert.That(fixture.Enrollments.All.Single(), Is.EqualTo(membershipBefore));
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
    }
}
