namespace Domain.Materials;

/// <summary>
/// Nastavni materijal vezan za konkretno izvođenje predmeta (ST-009).
/// Obuhvata naziv, opis, izvođenje i stanje objave. Prilozi i spoljne veze
/// nisu deo ovog modela; objava (US-08) je prelaz nacrt → objavljen,
/// povlačenje (US-09) prelaz objavljen → povučen, a povučen materijal
/// se može ponovo objaviti (US-09).
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
    public MaterialPublicationState PublicationState { get; private set; }

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

    /// <summary>
    /// Objava materijala (ST-009, US-08): prelaz iz nacrta u objavljen.
    /// Povučen materijal se može ponovo objaviti (US-09): prelaz povučen → objavljen.
    /// Već objavljen materijal se ne objavljuje ponovo: baca se
    /// <see cref="InvalidOperationException"/> i stanje se ne menja.
    /// Provera prava i postojanja datoteka priloga je odgovornost slučaja upotrebe.
    /// </summary>
    public void Publish()
    {
        if (PublicationState is not (MaterialPublicationState.Draft or MaterialPublicationState.Withdrawn))
            throw new InvalidOperationException("Objaviti se može samo nacrt ili povučen materijal.");

        PublicationState = MaterialPublicationState.Published;
    }

    /// <summary>
    /// Povlačenje materijala (ST-009, US-09): prelaz iz objavljen u povučen.
    /// Materijal se ne briše — naziv, opis, prilozi i veze ostaju sačuvani.
    /// Povući se može samo objavljen materijal; za svako drugo stanje baca se
    /// <see cref="InvalidOperationException"/> i stanje se ne menja.
    /// Provera prava je odgovornost slučaja upotrebe.
    /// </summary>
    public void Withdraw()
    {
        if (PublicationState != MaterialPublicationState.Published)
            throw new InvalidOperationException("Povući se može samo objavljen materijal.");

        PublicationState = MaterialPublicationState.Withdrawn;
    }
}
