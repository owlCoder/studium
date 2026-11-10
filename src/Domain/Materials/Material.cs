namespace Domain.Materials;

/// <summary>
/// Nastavni materijal vezan za konkretno izvođenje predmeta (ST-009).
/// Obuhvata naziv, opis, izvođenje i stanje objave. Prilozi, spoljne veze
/// i prelazi stanja objave nisu deo ovog modela i dodaju se posebnim zadacima.
/// </summary>
public sealed class Material
{
    public Guid Id { get; }

    /// <summary>
    /// Identifikator izvođenja predmeta (ne kataloškog predmeta).
    /// </summary>
    public Guid CourseOfferingId { get; }

    public string Name { get; }
    public string Description { get; }
    public MaterialPublicationState PublicationState { get; }

    public Material(Guid id, Guid courseOfferingId, string name, string? description)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator materijala mora biti zadat.", nameof(id));
        if (courseOfferingId == Guid.Empty)
            throw new ArgumentException("Izvođenje predmeta mora biti zadato.", nameof(courseOfferingId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv materijala je obavezan.", nameof(name));

        Id = id;
        CourseOfferingId = courseOfferingId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        PublicationState = MaterialPublicationState.Draft;
    }
}
