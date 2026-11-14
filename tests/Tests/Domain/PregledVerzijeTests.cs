using System;
using NUnit.Framework;
using Studium.Domain.Reviews;

namespace Studium.Tests.Domain
{
    [TestFixture]
    public class PregledVerzijeTests
    {
        [Test]
        public void Pregled_PamtiIdKonkretneVerzijePredaje()
        {
            
            var verzija1 = new VerzijaPredaje(Guid.NewGuid(), Guid.NewGuid(), 1);
            
            var pregled = new PregledVerzije(Guid.NewGuid(), verzija1.Id, "Dobar rad", "Prihvaćen");

            // Da li je sačuvan tačan ID te konkretne predaje
            Assert.That(pregled.SubmissionId, Is.EqualTo(verzija1.Id), 
                "Pregled mora pamtiti ID konkretne verzije predaje.");
        }

        [Test]
        public void NovaVerzijaPredaje_NeMenjaVezuPostojecegPregleda()
        {
            
            var prvaVerzija = new VerzijaPredaje(Guid.NewGuid(), Guid.NewGuid(), 1);
            var pregled = new PregledVerzije(Guid.NewGuid(), prvaVerzija.Id, "Ima grešaka", "Dorada");

        
            var novaVerzija = new VerzijaPredaje(Guid.NewGuid(), prvaVerzija.ZadatakId, 2);

            
            Assert.That(pregled.SubmissionId, Is.EqualTo(prvaVerzija.Id), 
                "Veza postojećeg pregleda mora ostati netaknuta.");
            
            
            Assert.That(pregled.SubmissionId, Is.Not.EqualTo(novaVerzija.Id), 
                "Nova verzija predaje ne sme da promeni vezu na već postojećem pregledu.");
        }
    }
}