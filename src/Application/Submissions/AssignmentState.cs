namespace Application.Submissions;

/// <summary>
/// Stanje zadatka koje je potrebno predaji rada (ST-012).
/// Predaja je dozvoljena samo za zadatak u stanju Published.
/// </summary>
public enum AssignmentState
{
    Draft,
    Published,
    Closed
}
