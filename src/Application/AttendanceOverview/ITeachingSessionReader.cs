using Domain.TeachingSessions;

namespace Application.AttendanceOverview;

/// <summary>
/// Port za čitanje termina nastave jednog izvođenja (ST-034, skladište D3).
/// <para>
/// Postojeći <c>ISessionRepository</c> (ST-031) vraća termine samo za jedan datum,
/// a pregled prisustva obuhvata ceo period izvođenja. Zato ST-034 ima svoj port
/// samo za čitanje; implementaciju nad stvarnim skladištem obezbeđuje ST-031.
/// </para>
/// </summary>
public interface ITeachingSessionReader
{
    /// <summary>
    /// Svi termini izvođenja, u svim stanjima (Planiran, Održan, Otkazan).
    /// Izbor održanih i podobnih termina radi handler.
    /// </summary>
    Task<IReadOnlyList<TeachingSession>> ListByOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
