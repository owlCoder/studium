using NUnit.Framework;
using Studium.Domain.ST016.Enums;

namespace Studium.Tests.Domain.ST016;

[TestFixture]
public class VrstaAktivnostiTests
{
    [Test]
    public void VrstaAktivnosti_SadrziSveOcekivaneVrednosti()
    {
        // Assert: Proveravamo da li su sve definisane konstante prisutne u enumu
        Assert.That(Enum.IsDefined(typeof(VrstaAktivnosti), VrstaAktivnosti.Zadatak), Is.True);
        Assert.That(Enum.IsDefined(typeof(VrstaAktivnosti), VrstaAktivnosti.Predavanje), Is.True);
        Assert.That(Enum.IsDefined(typeof(VrstaAktivnosti), VrstaAktivnosti.Vezbe), Is.True);
        Assert.That(Enum.IsDefined(typeof(VrstaAktivnosti), VrstaAktivnosti.Odbrana), Is.True);
    }

    [Test]
    public void VrstaAktivnosti_ParsiranjeIzStringa_PrepoznajeTacneVrednosti()
    {
        // Act & Assert: Proveravamo da li se API stringovi uspešno parsiraju u enum
        Assert.That(Enum.Parse<VrstaAktivnosti>("Zadatak"), Is.EqualTo(VrstaAktivnosti.Zadatak));
        Assert.That(Enum.Parse<VrstaAktivnosti>("Predavanje"), Is.EqualTo(VrstaAktivnosti.Predavanje));
        Assert.That(Enum.Parse<VrstaAktivnosti>("Vezbe"), Is.EqualTo(VrstaAktivnosti.Vezbe));
        Assert.That(Enum.Parse<VrstaAktivnosti>("Odbrana"), Is.EqualTo(VrstaAktivnosti.Odbrana));
    }
}