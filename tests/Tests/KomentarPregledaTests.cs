using System;
using Xunit;
using Studium.Domain.Models;

public class KomentarPregledaTests
{
    [Fact]
    public void Kreiraj_KomentarSaValidnimPodacima_UspesnoKreira()
    {
        // Arrange & Act
        var komentar = new KomentarPregleda("Proveriti sintaksnu analizu", 1);

        // Assert
        Assert.NotEqual(Guid.Empty, komentar.Id);
        Assert.Equal("Proveriti sintaksnu analizu", komentar.Tekst);
        Assert.Equal(1, komentar.RedniBroj);
    }

    [Fact]
    public void Kreiraj_KomentarPrazanTekst_BacaIzuzetak()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new KomentarPregleda("", 1));
        Assert.Throws<ArgumentException>(() => new KomentarPregleda("   ", 2));
    }
}