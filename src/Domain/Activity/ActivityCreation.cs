namespace Domain.Activity;

/// <summary>
/// Rezultat kreiranja aktivnosti. Domen ne baca izuzetke za neispravne podatke,
/// već vraća grešku koju sloj iznad prevodi u odgovor klijentu.
/// </summary>
public sealed record ActivityCreation(Activity? Activity, string? Error)
{
    public bool IsSuccess => Activity is not null;

    public static ActivityCreation Success(Activity activity) =>
        new(activity, null);

    public static ActivityCreation Failure(string error) =>
        new(null, error);
}
