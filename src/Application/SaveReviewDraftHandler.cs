using System;
using Studium.Domain.Models;

namespace Studium.Application
{
    // 1. Interfejs (Port) za repozitorijum
    public interface IReviewRepository
    {
        void Save(PregledRada pregled);
        PregledRada? GetByRadId(Guid radId);
    }

    // 2. Handler koji čuva i ažurira nacrt
    public class SaveReviewDraftHandler
    {
        private readonly IReviewRepository _repository;

        public SaveReviewDraftHandler(IReviewRepository repository)
        {
            _repository = repository;
        }

        public bool Handle(SaveReviewDraftCommand komanda)
        {
            // Validacija ulaznih podataka
            if (string.IsNullOrWhiteSpace(komanda.Komentar) || string.IsNullOrWhiteSpace(komanda.AutorId))
            {
                return false;
            }

            var pregled = _repository.GetByRadId(komanda.RadId);

            if (pregled == null)
            {
                // Ako pregled ne postoji, kreira se novi (Stanje je automatski Nacrt)
                pregled = new PregledRada(komanda.RadId, komanda.Komentar, komanda.AutorId, komanda.Outcome);
            }
            else
            {
                // Kontrola vlasništva: samo nastavnik koji je kreirao nacrt može da ga menja
                if (pregled.AutorId != komanda.AutorId)
                {
                    return false;
                }

                try
                {
                    // Ako postoji i vlasnik je isti, ažurira se nacrt
                    pregled.AzurirajNacrt(komanda.Komentar, komanda.AutorId, komanda.Outcome);
                }
                catch (InvalidOperationException)
                {
                    // Ukoliko pokušamo ažurirati objavljen pregled, metoda baca izuzetak.
                    return false;
                }
            }

            _repository.Save(pregled);
            return true;
        }
    }
}