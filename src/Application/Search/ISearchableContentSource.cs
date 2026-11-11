using Domain.Search;

namespace Application.Search;

/// <summary>
/// Izvor jedne vrste sadržaja za pretragu, skladište D3 (ST-018, US-5).
/// Nova vrsta sadržaja dodaje se novom klasom, bez izmene pretrage.
/// </summary>
public interface ISearchableContentSource
{
    ContentType Type { get; }

    /// <summary>
    /// Vraća stavke čiji naziv ili opis sadrži tekst, opciono samo za jedno izvođenje.
    /// Pravila pristupa se ovde ne primenjuju.
    /// </summary>
    Task<IReadOnlyList<SearchableItem>> SearchAsync(
        string text,
        Guid? offeringId,
        CancellationToken cancellationToken = default);
}
