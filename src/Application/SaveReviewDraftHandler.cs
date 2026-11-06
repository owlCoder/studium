using System;
using System.Collections.Generic;
using System.Linq;
using Studium.Domain.Models; 

namespace Studium.Application
{
    // 1. Interfejs za repozitorij
    public interface IReviewRepository
    {
        void Save(PregledRada pregled);
        PregledRada GetByRadId(Guid radId);
    }

    // 2. In-memory repozitorij
    public class InMemoryReviewRepository : IReviewRepository
    {
        private readonly List<PregledRada> _pregledi = new();

        public void Save(PregledRada pregled)
        {
            var postojeci = GetByRadId(pregled.RadId);
            if (postojeci != null)
            {
                _pregledi.Remove(postojeci);
            }
            _pregledi.Add(pregled);
        }

        public PregledRada GetByRadId(Guid radId)
        {
            return _pregledi.FirstOrDefault(p => p.RadId == radId);
        }
    }

    // 3. Handler koji čuva nacrt
    public class SaveReviewDraftHandler
    {
        private readonly IReviewRepository _repository;

        public SaveReviewDraftHandler(IReviewRepository repository)
        {
            _repository = repository;
        }

        public bool Handle(SaveReviewDraftCommand komanda)
        {
            // Odbijen unos ne menja postojeće stanje u bazi/memoriji
            if (string.IsNullOrWhiteSpace(komanda.Komentar) || string.IsNullOrWhiteSpace(komanda.AutorId))
            {
                return false; 
            }

            var pregled = _repository.GetByRadId(komanda.RadId);
            
            if (pregled == null)
            {
                // Ako pregled ne postoji, kreira se novi koristeći Marinin konstruktor (Stanje je automatski Nacrt)
                pregled = new PregledRada(komanda.RadId, komanda.Komentar, komanda.AutorId, komanda.Outcome);
            }
            else
            {
                try
                {
                    // Ako postoji, ažurira se koristeći njenu metodu
                    pregled.AzurirajNacrt(komanda.Komentar, komanda.AutorId, komanda.Outcome);
                }
                catch (InvalidOperationException)
                {
                    // Ukoliko pokušamo ažurirati objavljen pregled, metoda baca izuzetak.
                    // Vraćamo false i ne snimamo ništa, postojeće stanje ostaje netaknuto
                    return false;
                }
            }

            _repository.Save(pregled);
            return true;
        }
    }
}