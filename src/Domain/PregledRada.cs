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
    public ReviewOutcome? Outcome { get; private set; }
    public ReviewState State { get; private set; }
    public DateTime? NoviRok { get; private set; }

    public PregledRada(Guid radId, string komentar)
    {
        Id = Guid.NewGuid();
        RadId = radId;
        Komentar = komentar;
        State = ReviewState.Nacrt;
    }

    // Poslovno pravilo: Objavljivanje pregleda uz proveru novog roka
    public void Objavi(ReviewOutcome outcome, DateTime? noviRok = null)
    {
        if (outcome == ReviewOutcome.PotrebnaDorada && (!noviRok.HasValue || noviRok.Value <= DateTime.UtcNow))
        {
            throw new InvalidOperationException("Ishod 'Potrebna dorada' zahteva definisan novi rok u budućnosti.");
        }

        Outcome = outcome;
        NoviRok = noviRok;
        State = ReviewState.Objavljen;
    }
}