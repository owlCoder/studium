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

    public string Name { get; private set; }
    public string Description { get; private set; }
    public MaterialPublicationState PublicationState { get; }

    /// <summary>
    /// Vreme poslednje uspešne izmene naziva ili opisa (ST-009, US-02), u UTC.
    /// <c>null</c> znači da materijal nije menjan od kreiranja.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; private set; }

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

    /// <summary>
    /// Izmena naziva i opisa postojećeg materijala (ST-009, US-02).
    /// Važe ista pravila kao pri kreiranju: naziv je obavezan, opis nije.
    /// Validacija prethodi dodeli, pa neuspela izmena ne menja postojeće stanje
    /// (ni naziv, ni opis, ni vreme poslednje izmene).
    /// </summary>
    /// <param name="modifiedAt">Trenutak izmene; zadaje ga pozivalac (izvor vremena).</param>
    public void UpdateDetails(string name, string? description, DateTimeOffset modifiedAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv materijala je obavezan.", nameof(name));
        if (modifiedAt == default)
            throw new ArgumentException("Vreme izmene mora biti zadato.", nameof(modifiedAt));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        LastModifiedAt = modifiedAt.ToUniversalTime();
    }
}
