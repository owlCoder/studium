using System;
using Xunit;
using Studium.Domain.Models;

public class PregledRadaTests
{
    [Fact]
    public void Objavi_PotrebnaDoradaBezRoka_BacaIzuzetak()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Potrebne su izmene u kodu.", "nastavnik123");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            pregled.Objavi(ReviewOutcome.PotrebnaDorada, noviRok: null));
    }

    [Fact]
    public void Objavi_PotrebnaDoradaSaValidnimRokom_UspesnoObjavljuje()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Potrebne su izmene.", "nastavnik123");
        var noviRok = DateTime.UtcNow.AddDays(7);

        // Act
        pregled.Objavi(ReviewOutcome.PotrebnaDorada, noviRok);

        // Assert
        Assert.Equal(ReviewState.Objavljen, pregled.State);
        Assert.Equal(ReviewOutcome.PotrebnaDorada, pregled.Outcome);
        Assert.Equal(noviRok, pregled.NoviRok);
    }

    [Fact]
    public void Objavi_PrihvacenBezRoka_UspesnoObjavljuje()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Rad je odličan.", "nastavnik123");

        // Act
        pregled.Objavi(ReviewOutcome.Prihvacen);

        // Assert
        Assert.Equal(ReviewState.Objavljen, pregled.State);
        Assert.Equal(ReviewOutcome.Prihvacen, pregled.Outcome);
    }

    [Fact]
    public void DodajStavkuKomentara_ValidniPodaci_UspesnoDodajeStavku()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Glavni komentar.", "nastavnik123");

        // Act
        pregled.DodajStavkuKomentara("Proveriti sintaksnu analizu.", 1);

        // Assert
        var stavka = Assert.Single(pregled.StavkeKomentara);
        Assert.Equal("Proveriti sintaksnu analizu.", stavka.Tekst);
        Assert.Equal(1, stavka.RedniBroj);
    }

    [Fact]
    public void AzurirajNacrt_DokJeNacrt_UspesnoAzurira()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Stari komentar", "nastavnik1");

        // Act
        pregled.AzurirajNacrt("Novi ažurirani komentar", "nastavnik2", ReviewOutcome.Prihvacen);

        // Assert
        Assert.Equal("Novi ažurirani komentar", pregled.Komentar);
        Assert.Equal("nastavnik2", pregled.AutorId);
        Assert.Equal(ReviewOutcome.Prihvacen, pregled.Outcome);
    }
}