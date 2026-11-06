using System.Text;

namespace Application.Common;

/// <summary>
/// Kratka, čitljiva oznaka entiteta za URL: prefiks (oznaka ili naziv) i poslednjih
/// pet znakova identifikatora. Primer: predmet ERS sa identifikatorom
/// <c>11111111-1111-1111-1111-111111111111</c> daje <c>ERS-11111</c>.
/// </summary>
public static class EntitySlug
{
    /// <summary>Broj završnih znakova identifikatora koji ulaze u oznaku.</summary>
    public const int IdSuffixLength = 5;

    /// <summary>Sastavlja oznaku od prefiksa i završetka identifikatora.</summary>
    public static string Create(string? prefix, Guid id)
    {
        var normalizedPrefix = Normalize(prefix);
        var suffix = IdSuffix(id);
        return normalizedPrefix.Length == 0 ? suffix : $"{normalizedPrefix}-{suffix}";
    }

    /// <summary>Vraća poslednjih <see cref="IdSuffixLength"/> znakova identifikatora (bez crtica).</summary>
    public static string IdSuffix(Guid id)
    {
        var value = id.ToString("N");
        return value[^IdSuffixLength..];
    }

    /// <summary>Proverava da li oznaka odgovara datom prefiksu i identifikatoru, bez obzira na velika i mala slova.</summary>
    public static bool Matches(string? slug, string? prefix, Guid id) =>
        string.Equals(slug?.Trim(), Create(prefix, id), StringComparison.OrdinalIgnoreCase);

    // Zadržava slova, cifre, '-' '_' i '.', a sve ostalo (razmaci, '/' i sl.) pretvara
    // u jednu crticu, bez vodećih i završnih crtica.
    private static string Normalize(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(prefix.Length);
        foreach (var character in prefix.Trim())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_' or '.')
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        while (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}
