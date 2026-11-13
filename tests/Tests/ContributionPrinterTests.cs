using Application.Contributions;
using ConsoleUi;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class ContributionPrinterTests
{
    [Test]
    public void PrintList_Prazna_IspisujePoruku()
    {
        var writer = new StringWriter();

        ContributionPrinter.PrintList(Array.Empty<ContributionSummaryDto>(), writer);

        Assert.That(writer.ToString(), Does.Contain("Nema evidentiranih doprinosa"));
    }

    [Test]
    public void PrintDetails_SaIstorijom_IspisujeObeVerzije()
    {
        var first = new ContributionRevisionDto(1, "Prvi opis", DateTime.Now, "Dopuni", Array.Empty<EvidenceDto>());
        var second = new ContributionRevisionDto(2, "Drugi opis", DateTime.Now, "", Array.Empty<EvidenceDto>());
        var dto = new ContributionDetailsDto(1, 101, 10, 1, "Podnet", DateTime.Now, DateTime.Now, second, new[] { first, second });
        var writer = new StringWriter();

        ContributionPrinter.PrintDetails(dto, writer);

        var text = writer.ToString();
        Assert.That(text, Does.Contain("Prvi opis"));
        Assert.That(text, Does.Contain("Drugi opis"));
        Assert.That(text, Does.Contain("Napomena nastavnika: Dopuni"));
        Assert.That(text, Does.Contain("zaduženje: 101"));
        Assert.That(text, Does.Not.Contain("verifikovan"));
    }
}
