namespace Api;

/// <summary>
/// Čita token iz zaglavlja "Authorization: Bearer ...". Pripada Api sloju jer zna za HTTP;
/// Application dobija samo tekst tokena.
/// </summary>
public static class BearerToken
{
    private const string Prefix = "Bearer ";

    public static string? Read(HttpRequest request)
    {
        string? header = request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string token = header[Prefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }
}
