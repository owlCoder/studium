namespace Application.Access;

/// <summary>
/// Čitanje članstava i angažovanja na izvođenjima, skladište D2 (ST-018, US-6).
/// </summary>
public interface IMembershipReader
{
    /// <summary>
    /// Da li korisnik ima aktivno studentsko članstvo na izvođenju.
    /// </summary>
    Task<bool> IsActiveStudentAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Da li je korisnik nastavnik ili asistent angažovan na izvođenju.
    /// </summary>
    Task<bool> IsEngagedStaffAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
