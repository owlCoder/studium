namespace Application.Submissions;

/// <summary>
/// Daje trenutno vreme (ST-012).
/// Handler ne koristi DateTimeOffset.UtcNow direktno,
/// da bismo u testovima mogli da zadamo tačno vreme predaje.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
