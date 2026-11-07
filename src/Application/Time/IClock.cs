namespace Application.Time;

/// <summary>
/// Jedinstven izvor vremena (stavka 2.4). Ceo kod koristi ovaj port umesto DateTime.Now / DateTime.UtcNow,
/// da bi se istek tokena i slična pravila mogli testirati lažnim satom.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
