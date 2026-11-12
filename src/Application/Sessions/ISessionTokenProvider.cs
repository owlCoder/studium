namespace Application.Sessions;

/// <summary>
/// Port: pravljenje tokena i njegovog heša.
/// </summary>
public interface ISessionTokenProvider
{
    /// <summary>Pravi novi nepredvidiv token. Svaki poziv vraća drugačiju vrednost.</summary>
    string Generate();

    /// <summary>Isti token uvek daje isti heš; heš se ne može vratiti u token.</summary>
    string Hash(string token);
}
