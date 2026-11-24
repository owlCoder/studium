namespace Domain.Materials;

/// <summary>
/// Pravilo vidljivosti materijala za studente (ST-009, „Zaštita pristupa materijalu
/// i prilozima”, tačka 2): student vidi samo objavljen materijal. Nacrt i povučen
/// materijal nisu dostupni studentu ni preko direktne adrese.
/// Nastavno osoblje izvođenja nije ograničeno ovim pravilom.
/// </summary>
public static class MaterialVisibility
{
    /// <summary>
    /// <c>true</c> samo za <see cref="MaterialPublicationState.Published"/>.
    /// Svako drugo stanje, uključujući i buduća, je za studente nevidljivo.
    /// </summary>
    public static bool IsVisibleToStudents(this MaterialPublicationState state) =>
        state == MaterialPublicationState.Published;
}
