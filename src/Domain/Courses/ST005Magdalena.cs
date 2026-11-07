// Doprinos za KT1 Magdalena Popović
// Tema: ST-005 Odgovorni nastavnik

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Studium.Domain.Courses
{
    /// <summary>
    /// ST-005: Entitet koji predstavlja angažovanje nastavnog osoblja na specifičnom izvođenju predmeta.
    /// Ovo je domenski entitet koji definiše 'Odgovornog nastavnika'.
    /// </summary>
    public class AngazovanjeNastavnika
    {
        // Jedinstveni identifikator angažovanja (primary key)
        public Guid Id { get; set; } = Guid.NewGuid();

        // Identifikator specifičnog izvođenja predmeta (foreign key)
        public Guid IzvodjenjeId { get; set; }

        // Identifikator korisnika (nastavnika/asistenta) koji je angažovan (foreign key)
        public Guid KorisnikId { get; set; }

        // ISPRVARENA GREŠKA: Ime svojstva je spojeno (UlogaNaPredmetu), jer C# ne dozvoljava razmake.
        // Uloga korisnika na ovom izvođenju (npr. "Odgovorni", "Nastavnik", "Asistent")
        public string UlogaNaPredmetu { get; set; }


        // --- Komentar doprinosa za KT1 ---
        // Magdalena Popović: Započeta definicija domenskog entiteta na osnovu specifikacije ST-005.
    }
}
