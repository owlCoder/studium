using Domain.Activity;

namespace Application.Activities;

/// <summary>
/// Port za čuvanje i čitanje aktivnosti. Memorijski adapter je demonstracioni;
/// trajno skladište treba da zameni implementaciju bez izmene slučajeva upotrebe.
/// </summary>
public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Activity>> ListAsync(CancellationToken cancellationToken = default);
}
