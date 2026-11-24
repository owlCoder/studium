using Domain.Activity;

namespace Application.Activities;

/// <summary>
/// Port za čuvanje i čitanje aktivnosti. Memorijski adapter je demonstracioni;
/// trajno skladište treba da zameni implementaciju bez izmene slučajeva upotrebe.
/// Implementacije ne bacaju izuzetke; neuspeh i otkazivanje vraćaju se kao
/// <see cref="ActivityAddResult"/> odnosno <see cref="ActivityListResult"/>.
/// </summary>
public interface IActivityRepository
{
    /// <summary>
    /// Čuva aktivnost. <c>null</c> se prijavljuje kao
    /// <see cref="ActivityAddStatus.InvalidActivity"/>, a otkazan zahtev kao
    /// <see cref="ActivityAddStatus.Canceled"/>.
    /// </summary>
    Task<ActivityAddResult> AddAsync(Activity? activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća sve aktivnosti. Otkazan zahtev daje
    /// <see cref="ActivityListStatus.Canceled"/> sa praznom kolekcijom.
    /// </summary>
    Task<ActivityListResult> ListAsync(CancellationToken cancellationToken = default);
}
