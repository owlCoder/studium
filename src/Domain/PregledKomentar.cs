using System;

namespace Studium.Domain.Models
{
    public class KomentarPregleda
    {
        public Guid Id { get; private set; }
        public string Tekst { get; private set; }
        public int RedniBroj { get; private set; }

        public KomentarPregleda(string tekst, int redniBroj)
        {
            if (string.IsNullOrWhiteSpace(tekst))
            {
                throw new ArgumentException("Tekst komentara ne može biti prazan.", nameof(tekst));
            }

            Id = Guid.NewGuid();
            Tekst = tekst;
            RedniBroj = redniBroj;
        }
    }
}