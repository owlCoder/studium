namespace Studium.Domain.ST015.Entities;

public sealed class StudentZadatakRok
{
    public Guid StudentId { get; private set; }
    public Guid ZadatakId { get; private set; }
    public DateTime? PojedinacniRok { get; private set; }

    private StudentZadatakRok() { }
    public StudentZadatakRok(Guid studentId, Guid zadatakId) => (StudentId, ZadatakId) = (studentId, zadatakId);

    public void PostaviPojedinacniRok(DateTime noviRok) => PojedinacniRok = noviRok;
    
    // Core pravilo: pojedinačni rok ima prednost
    public DateTime IzracunajPrimenljiviRok(DateTime zajednickiRok) => PojedinacniRok ?? zajednickiRok;
}