using System;
using Xunit;

public class PregledRadaTests
{
    [Fact]
    public void Objavi_PotrebnaDoradaBezRoka_BacaIzuzetak()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Potrebne su izmene u kodu.");

        // Act & Assert (Pravilo: „Potrebna dorada” bez novog roka nije dozvoljena)
        Assert.Throws<InvalidOperationException>(() => 
            pregled.Objavi(ReviewOutcome.PotrebnaDorada, noviRok: null));
    }

    [Fact]
    public void Objavi_PotrebnaDoradaSaValidnimRokom_UspesnoObjavljuje()
    {
        // Arrange
        var pregled = new PregledRada(Guid.NewGuid(), "Potrebne su izmene.");
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
        var pregled = new PregledRada(Guid.NewGuid(), "Rad je odličan.");

        // Act
        pregled.Objavi(ReviewOutcome.Prihvacen);

        // Assert
        Assert.Equal(ReviewState.Objavljen, pregled.State);
        Assert.Equal(ReviewOutcome.Prihvacen, pregled.Outcome);
    }
}