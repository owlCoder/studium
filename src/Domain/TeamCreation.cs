namespace Studium.Domain.Entities;

/// <summary>
/// Rezultat kreiranja tima. Domen ne baca izuzetke za neispravne podatke, već
/// vraća grešku koju sloj iznad prevodi u odgovor klijentu.
/// </summary>
public sealed record TeamCreation(StudentTeam? Team, string? Error)
{
    public bool IsSuccess => Team is not null;

    public static TeamCreation Success(StudentTeam team) =>
        new(team, null);

    public static TeamCreation Failure(string error) =>
        new(null, error);
}
