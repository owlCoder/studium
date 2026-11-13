using System;
using System.Collections.Generic;
using System.Linq;

namespace Studium.Domain.Courses
{
    /// <summary>
    /// ST-005: uloga nastavnog osoblja na konkretnom izvođenju predmeta.
    /// </summary>
    public enum UlogaNaPredmetu
    {
        OdgovorniNastavnik = 1,
        Nastavnik = 2,
        Asistent = 3
    }

    /// <summary>
    /// ST-005: angažovanje jednog korisnika na jednom izvođenju predmeta.
    /// Domenski entitet sa invarijantama; ne zavisi od drugih slojeva.
    /// Angažovanje se ne briše nego prekida (KrajUtc), da autorstvo ranijih zapisa ostane očuvano.
    /// </summary>
    public sealed class AngazovanjeNastavnika
    {
        public Guid Id { get; private set; }
        public Guid IzvodjenjeId { get; private set; }
        public Guid KorisnikId { get; private set; }
        public UlogaNaPredmetu UlogaNaPredmetu { get; private set; }
        public DateTime PocetakUtc { get; private set; }
        public DateTime? KrajUtc { get; private set; }

        public bool Aktivno => KrajUtc is null;

        // Rezervisano za kasniji ORM/migraciju (infrastruktura nije deo ovog doprinosa).
        private AngazovanjeNastavnika() { }

        /// <param name="pocetakUtc">
        /// Vreme dolazi spolja (jedan merodavan izvor vremena, spec. 2.4), ne iz DateTime.UtcNow.
        /// </param>
        public static AngazovanjeNastavnika Kreiraj(
            Guid izvodjenjeId,
            Guid korisnikId,
            UlogaNaPredmetu uloga,
            DateTime pocetakUtc)
        {
            if (izvodjenjeId == Guid.Empty)
                throw new ArgumentException("Izvođenje je obavezno.", nameof(izvodjenjeId));
            if (korisnikId == Guid.Empty)
                throw new ArgumentException("Korisnik je obavezan.", nameof(korisnikId));
            if (!Enum.IsDefined(typeof(UlogaNaPredmetu), uloga))
                throw new ArgumentException("Nepoznata uloga na predmetu.", nameof(uloga));
            if (pocetakUtc == default)
                throw new ArgumentException("Početak angažovanja je obavezan.", nameof(pocetakUtc));

            return new AngazovanjeNastavnika
            {
                Id = Guid.NewGuid(),
                IzvodjenjeId = izvodjenjeId,
                KorisnikId = korisnikId,
                UlogaNaPredmetu = uloga,
                PocetakUtc = pocetakUtc
            };
        }

        /// <summary>
        /// Prekida angažovanje. Identitet, izvođenje i korisnik se ne menjaju, pa autorstvo ostaje očuvano.
        /// Pravilo "mora ostati bar jedan odgovorni" proverava se u <see cref="OdgovorniNastavnikPravila"/>.
        /// </summary>
        public void Prekini(DateTime krajUtc)
        {
            if (!Aktivno)
                throw new InvalidOperationException("Angažovanje je već prekinuto.");
            if (krajUtc < PocetakUtc)
                throw new ArgumentOutOfRangeException(nameof(krajUtc), "Kraj ne može biti pre početka.");

            KrajUtc = krajUtc;
        }
    }

    /// <summary>
    /// ST-005: pravila koja zavise od skupa angažovanja jednog izvođenja.
    /// Čiste funkcije bez pristupa bazi; skup angažovanja i stanje izvođenja (ST-004) dobijaju se spolja.
    /// </summary>
    public static class OdgovorniNastavnikPravila
    {
        /// <summary>
        /// Ista osoba ne može imati dva aktivna angažovanja na istom izvođenju
        /// (projektna odluka tima; nije eksplicitno u specifikaciji).
        /// </summary>
        public static void ProveriDodjelu(
            IEnumerable<AngazovanjeNastavnika> postojecaAngazovanja,
            Guid izvodjenjeId,
            Guid korisnikId)
        {
            ArgumentNullException.ThrowIfNull(postojecaAngazovanja);

            bool vecAngazovan = postojecaAngazovanja.Any(a =>
                a.Aktivno && a.IzvodjenjeId == izvodjenjeId && a.KorisnikId == korisnikId);

            if (vecAngazovan)
                throw new InvalidOperationException("Korisnik je već angažovan na ovom izvođenju.");
        }

        /// <summary>
        /// Aktivno izvođenje mora imati bar jednog odgovornog nastavnika. Zato se prekid poslednjeg
        /// aktivnog odgovornog odbija, što pokriva i slučaj kada odgovorni pokuša da ukloni sebe.
        /// </summary>
        public static void ProveriPrekid(
            IEnumerable<AngazovanjeNastavnika> angazovanjaIzvodjenja,
            AngazovanjeNastavnika zaPrekid,
            bool izvodjenjeJeAktivno)
        {
            ArgumentNullException.ThrowIfNull(angazovanjaIzvodjenja);
            ArgumentNullException.ThrowIfNull(zaPrekid);

            if (!zaPrekid.Aktivno)
                throw new InvalidOperationException("Angažovanje je već prekinuto.");

            if (!izvodjenjeJeAktivno || zaPrekid.UlogaNaPredmetu != UlogaNaPredmetu.OdgovorniNastavnik)
                return;

            bool postojiDrugiOdgovorni = angazovanjaIzvodjenja.Any(a =>
                a.Aktivno
                && a.IzvodjenjeId == zaPrekid.IzvodjenjeId
                && a.UlogaNaPredmetu == UlogaNaPredmetu.OdgovorniNastavnik
                && a.Id != zaPrekid.Id);

            if (!postojiDrugiOdgovorni)
                throw new InvalidOperationException(
                    "Aktivno izvođenje mora imati najmanje jednog odgovornog nastavnika. " +
                    "Prvo odredite drugog odgovornog.");
        }
    }
}
