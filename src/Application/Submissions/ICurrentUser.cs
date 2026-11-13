namespace Application.Submissions;

/// <summary>
/// Daje podatke o korisniku koji je trenutno prijavljen (ST-012).
/// Prijava (ST-002) još ne postoji, pa ovaj interfejs samo opisuje šta nam treba.
/// Student koji predaje rad uzima se odavde, a ne iz komande.
/// </summary>
public interface ICurrentUser
{
    Guid StudentId { get; }
}
