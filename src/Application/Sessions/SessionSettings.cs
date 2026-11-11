namespace Application.Sessions;

/// <summary>
/// Podešavanja sesije. Trajanje sesije specifikacija ne propisuje, pa se bira u composition
/// root-u (Program.cs)
/// </summary>
public sealed class SessionSettings
{
    public TimeSpan Lifetime { get; }

    public SessionSettings(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime), "Trajanje sesije mora biti pozitivno.");
        }

        Lifetime = lifetime;
    }
}
