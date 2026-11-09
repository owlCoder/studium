namespace Domain.Notifications;

/// Veza obaveštenja ka izvornom zapisu. Veza ne daje pravo pristupa izvoru;
/// pristup se proverava posebno pri otvaranju (ST-017).

public sealed record SourceReference
{
    public SourceType Type { get; }
    public Guid Id { get; }

    public SourceReference(SourceType type, Guid id)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), "Nepoznata vrsta izvora.");
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator izvora mora biti zadat.", nameof(id));

        Type = type;
        Id = id;
    }
}
