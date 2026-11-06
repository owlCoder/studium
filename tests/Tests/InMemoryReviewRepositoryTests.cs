using System;
using Xunit;
using Studium.Domain.Models;
using Studium.Application;

namespace Studium.Tests
{
    public class InMemoryReviewRepositoryTests
    {
        [Fact]
        public void Save_DodajeNoviPregled_UkolikoNePostoji()
        {
            // Arrange (Priprema)
            var repo = new InMemoryReviewRepository();
            var radId = Guid.NewGuid();
            var pregled = new PregledRada(radId, "Neki komentar", "Autor123", ReviewOutcome.Prihvacen);

            // Act (Izvršenje)
            repo.Save(pregled);
            var sacuvani = repo.GetByRadId(radId);

            // Assert (Provjera)
            Assert.NotNull(sacuvani);
            Assert.Equal("Neki komentar", sacuvani.Komentar);
            Assert.Equal("Autor123", sacuvani.AutorId);
        }

        [Fact]
        public void Save_AzuriraPostojeciPregled_UkolikoRadIdVecPostoji()
        {
            // Arrange
            var repo = new InMemoryReviewRepository();
            var radId = Guid.NewGuid();
            var stariPregled = new PregledRada(radId, "Stari", "Autor1");
            repo.Save(stariPregled);
            
            var noviPregled = new PregledRada(radId, "Novi", "Autor2");

            // Act
            repo.Save(noviPregled);
            var sacuvani = repo.GetByRadId(radId);

            // Assert
            Assert.Equal("Novi", sacuvani.Komentar);
            Assert.Equal("Autor2", sacuvani.AutorId);
        }
    }
}