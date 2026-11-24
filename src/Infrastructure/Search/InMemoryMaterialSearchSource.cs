using Application.Search;
using Domain.Search;

namespace Infrastructure.Search;

/// <summary>
/// Memorijski izvor nastavnih materijala za pretragu (ST-018, US-5);
/// nema trajnog čuvanja podataka.
/// </summary>
public sealed class InMemoryMaterialSearchSource : ISearchableContentSource
{
    private readonly IReadOnlyList<SearchableItem> _materials;

    public InMemoryMaterialSearchSource(IEnumerable<SearchableItem> materials)
    {
        ArgumentNullException.ThrowIfNull(materials);
        _materials = materials.Where(m => m.Type == ContentType.Material).ToArray();
    }

    public ContentType Type => ContentType.Material;

    public Task<IReadOnlyList<SearchableItem>> SearchAsync(
        string text,
        Guid? offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var term = text?.Trim() ?? string.Empty;
        if (term.Length == 0)
            return Task.FromResult<IReadOnlyList<SearchableItem>>(Array.Empty<SearchableItem>());

        IReadOnlyList<SearchableItem> result = _materials
            .Where(m => offeringId is null || m.OfferingId == offeringId)
            .Where(m =>
                m.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(result);
    }
}
