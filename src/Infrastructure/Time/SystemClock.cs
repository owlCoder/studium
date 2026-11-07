using Application.Time;

namespace Infrastructure.Time;

/// <summary>Stvarni sat; jedino mesto u kodu koje sme da čita sistemsko vreme.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow
    {
        get { return DateTimeOffset.UtcNow; }
    }
}
