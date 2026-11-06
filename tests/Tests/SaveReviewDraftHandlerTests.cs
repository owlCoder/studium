using System;
using Xunit;
using Studium.Domain.Models;
using Studium.Application;

namespace Studium.Tests
{
    public class SaveReviewDraftHandlerTests
    {
        [Fact]
        public void Handle_KreiraNoviNacrt_I_BeleziAutora_KadaPregledNePostoji()
        {
            // Arrange
            var repo = new InMemoryReviewRepository();
            var handler = new SaveReviewDraftHandler(repo);
            var radId = Guid.NewGuid();
            
            var komanda = new SaveReviewDraftCommand 
            { 
                RadId = radId, 
                Komentar = "Detaljan pregled rada", 
                AutorId = "NastavnikMika", 
                Outcome = ReviewOutcome.Prihvacen 
            };

            // Act
            var uspesno = handler.Handle(komanda);
            var sacuvani = repo.GetByRadId(radId);

            // Assert
            Assert.True(uspesno);
            Assert.NotNull(sacuvani);
            // Provera da se pregled čuva kao nacrt[cite: 4]
            Assert.Equal(ReviewState.Nacrt, sacuvani.State); 
            // Provera da se autor pregleda beleži[cite: 4]
            Assert.Equal("NastavnikMika", sacuvani.AutorId); 
        }

        [Theory]
        [InlineData("", "Nastavnik1")] // Prazan komentar
        [InlineData("Dobar rad", "")]  // Prazan autor
        [InlineData(null, "Nastavnik1")]
        public void Handle_OdbijaUnos_KadaNedostajuPodaci_NeMenjaPostojeceStanje(string neispravanKomentar, string neispravanAutor)
        {
            // Arrange
            var repo = new InMemoryReviewRepository();
            var radId = Guid.NewGuid();
            
            // Unosimo početno, validno stanje
            var pocetnoStanje = new PregledRada(radId, "Originalni komentar", "OriginalniAutor", ReviewOutcome.PotrebnaDorada);
            repo.Save(pocetnoStanje); 

            var handler = new SaveReviewDraftHandler(repo);
            var neispravnaKomanda = new SaveReviewDraftCommand 
            { 
                RadId = radId, 
                Komentar = neispravanKomentar, 
                AutorId = neispravanAutor 
            };

            // Act
            var uspesno = handler.Handle(neispravnaKomanda);
            var stanjeNakonPokusaja = repo.GetByRadId(radId);

            // Assert
            Assert.False(uspesno); // Handler je odbio unos
            
            // Provera da odbijen unos ne menja postojeće stanje[cite: 4]
            Assert.Equal("Originalni komentar", stanjeNakonPokusaja.Komentar); 
            Assert.Equal("OriginalniAutor", stanjeNakonPokusaja.AutorId);
            Assert.Equal(ReviewOutcome.PotrebnaDorada, stanjeNakonPokusaja.Outcome);
        }
    }
}