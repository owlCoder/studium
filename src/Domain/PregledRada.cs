namespace Studium.Domain.Models
{
    public enum StanjePregleda
    {
        Nacrt,
        Objavljen
    }

    public enum IshodPregleda
    {
        Prihvacen,
        PotrebnaDorada
    }

    public enum StatusPregledaFilter
    {
        BezPredaje,
        Nepregledano,
        Pregledano
    }

    public class PregledRada
    {
        public int Id { get; set; }
        public int PredajaId { get; set; } // Veza sa važećom predajom studenta
        public string Komentar { get; set; }
        public IshodPregleda? Ishod { get; set; }
        public StanjePregleda Stanje { get; set; } = StanjePregleda.Nacrt; // Podrazumevano je nacrt (student ne vidi)
        public string AutorPregledaId { get; set; } // Nastavno osoblje koje je kreiralo pregled
        public DateTime? NoviRok { get; set; } // Navodi se ukoliko je potrebna dorada
        public DateTime DatumIzmene { get; set; } = DateTime.UtcNow;
    }
}