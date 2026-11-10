namespace Application.StudentImport;

/// <summary>Поређење корисничких имена у NFC облику, без разлике у величини слова.</summary>
/// <remarks>Парсер уклања околни празан простор и примењује NFC; чува величину слова и дијакритике.</remarks>
public static class UsernameComparison
{
    public static readonly StringComparer Comparer = new NfcComparer();

    public static string Key(string username) => username.Normalize().Trim();

    /// <summary>Своди канонски еквивалентне записе на NFC, без промене величине слова.</summary>
    public static string Canonical(string username)
        => string.IsNullOrEmpty(username) ? username : username.Normalize();

    private sealed class NfcComparer : StringComparer
    {
        public override int Compare(string? x, string? y)
            => OrdinalIgnoreCase.Compare(x?.Normalize(), y?.Normalize());

        public override bool Equals(string? x, string? y)
            => OrdinalIgnoreCase.Equals(x?.Normalize(), y?.Normalize());

        public override int GetHashCode(string value)
            => OrdinalIgnoreCase.GetHashCode(value.Normalize());
    }
}
