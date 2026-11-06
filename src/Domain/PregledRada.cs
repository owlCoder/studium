using System;
using System.Collections.Generic;

namespace Studium.Domain.Models
{
    public enum ReviewOutcome
    {
        Prihvacen,
        PotrebnaDorada
    }

    public enum ReviewState
    {
        Nacrt,
        Objavljen
    }

    public class PregledRada
    {
        public Guid Id { get; private set; }
        public Guid RadId { get; private set; }
        public string Komentar { get; private set; }
        public string AutorId { get; private set; } 
        public ReviewOutcome? Outcome { get; private set; }
        public ReviewState State { get; private set; }
        public DateTime? NoviRok { get; private set; }
        
        private readonly List<KomentarPregleda> _stavkeKomentara = new();
        public IReadOnlyCollection<KomentarPregleda> StavkeKomentara => _stavkeKomentara.AsReadOnly();

        
        public PregledRada(Guid radId, string komentar, string autorId, ReviewOutcome? outcome = null)
        {
            Id = Guid.NewGuid();
            RadId = radId;
            Komentar = komentar;
            AutorId = autorId;
            Outcome = outcome;
            State = ReviewState.Nacrt;
        }

        public void AzurirajNacrt(string komentar, string autorId, ReviewOutcome? outcome)
        {
            if (State != ReviewState.Nacrt)
            {
                throw new InvalidOperationException("Moguće je ažurirati samo nacrt.");
            }
            
            Komentar = komentar;
            AutorId = autorId;
            Outcome = outcome;
        }

        public void DodajStavkuKomentara(string tekst, int redniBroj)
        {
            var stavka = new KomentarPregleda(tekst, redniBroj);
            _stavkeKomentara.Add(stavka);
        }

        public void Objavi(ReviewOutcome outcome, DateTime? noviRok = null)
        {
            if (outcome == ReviewOutcome.PotrebnaDorada && (!noviRok.HasValue || noviRok.Value <= DateTime.UtcNow))
            {
                throw new InvalidOperationException("Za ishod 'Potrebna dorada' mora biti definisan novi rok u budućnosti.");
            }

            Outcome = outcome;
            NoviRok = noviRok;
            State = ReviewState.Objavljen;
        }
    }
}