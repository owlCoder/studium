using Application.Access;
using Domain.Search;

namespace Application.Search;

/// <summary>
/// Zajednički oblik sadržaja koji se pretražuje: izvođenje, obaveštenje,
/// materijal ili zadatak, sa stanjem Nacrt/Objavljeno/Povučeno (ST-018, US-5).
/// </summary>
public sealed record SearchableItem
{
    public Guid Id { get; }
    public ContentType Type { get; }
    public Guid OfferingId { get; }
    public string Title { get; }
    public string Description { get; }
    public ContentState State { get; }

    public SearchableItem(
        Guid id,
        ContentType type,
        Guid offeringId,
        string title,
        string description,
        ContentState state)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator sadržaja mora biti zadat.", nameof(id));
        if (offeringId == Guid.Empty)
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Naziv sadržaja je obavezan.", nameof(title));

        Id = id;
        Type = type;
        OfferingId = offeringId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        State = state;
    }
}
