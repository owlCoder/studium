namespace Studium.Domain.ST015.Entities;

public sealed class Zadatak
{
    public Guid Id { get; private set; }
    public DateTime ZajednickiRok { get; private set; }
    public bool DozvoljenoKasnjenje { get; private set; }
    public DateTime? DodatniDatumZaKasnjenje { get; private set; }

    private Zadatak() { }
    public Zadatak(Guid id, DateTime zajednickiRok) => (Id, ZajednickiRok) = (id, zajednickiRok);

    public void DozvoliZakasnelePredaje(DateTime doDatuma)
    {
        DozvoljenoKasnjenje = true;
        DodatniDatumZaKasnjenje = doDatuma;
    }
}