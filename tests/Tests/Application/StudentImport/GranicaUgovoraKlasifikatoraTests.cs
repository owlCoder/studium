using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Граница уговора између класификатора и потврде.</summary>
/// <remarks>Ред ван улаза мора бити пријављен као повреда уговора.</remarks>
[TestFixture]
public sealed class GranicaUgovoraKlasifikatoraTests
{
    /// <summary>Класификатор који врати ред који није у улазу.</summary>
    private sealed class RedIzVanUlazaKlasifikator : IImportRowClassifier
    {
        public Task<IReadOnlyList<ImportRowResult>> ClassifyAsync(
            IReadOnlyList<ParsedStudentRow> rows,
            Guid offeringId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ImportRowResult>>(
                new[] { new ImportRowResult(9999, "nepostojeci", RowStatus.Ispravan, string.Empty) });
    }

    [Test]
    public async Task Red_Koji_Nije_U_Ulazu_Daje_Jasnu_Gresku()
    {
        var fixture = new StudentImportFixture();
        var handler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard,
            fixture.Parser,
            new RedIzVanUlazaKlasifikator(),
            fixture.Processor,
            fixture.UnitOfWork,
            fixture.Clock);

        var error = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.HandleAsync(fixture.OfferingId, "firstname,lastname,username\nAna,Anić,ana.anic\n"));

        Assert.That(error!.Message, Does.Contain("9999"));
        Assert.That(error.Message, Does.Contain("Klasifikator"));
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        Assert.That(fixture.Enrollments.Count, Is.Zero);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
    }

}
