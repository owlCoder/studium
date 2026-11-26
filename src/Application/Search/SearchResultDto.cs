using Domain.Search;

namespace Application.Search;

/// <summary>
/// Jedan rezultat pretrage za prikaz korisniku (ST-018, US-7):
/// naziv, vrsta, kratak odlomak opisa i veza ka izvoru.
/// </summary>
public sealed record SearchResultDto(
    Guid Id,
    string Title,
    ContentType Type,
    string Snippet,
    string Link)
{
    public static SearchResultDto FromItem(SearchableItem item, string term)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new SearchResultDto(
            item.Id,
            item.Title,
            item.Type,
            SnippetBuilder.Build(item.Description, term),
            LinkFor(item));
    }

    private static string LinkFor(SearchableItem item)
    {
        var section = item.Type switch
        {
            ContentType.CourseOffering => "offerings",
            ContentType.Announcement => "announcements",
            ContentType.Material => "materials",
            ContentType.Assignment => "assignments",
            _ => "content"
        };

        return $"/{section}/{item.Id}";
    }
}
