namespace Application.Materials;

/// <summary>
/// Razlog odbijanja priloga prema <see cref="AttachmentUploadPolicy"/> (ST-009, US-05).
/// </summary>
public enum AttachmentRejectionReason
{
    /// <summary>Ekstenzija datoteke nije među dozvoljenim tipovima.</summary>
    UnsupportedType,

    /// <summary>Datoteka je veća od dozvoljene maksimalne veličine.</summary>
    TooLarge
}

/// <summary>
/// Datoteka je odbijena jer ne zadovoljava ograničenje tipa ili veličine (ST-009, US-05).
/// Kada se ovaj izuzetak pojavi, prilog nije vezan za materijal i sadržaj nije sačuvan.
/// </summary>
public sealed class AttachmentRejectedException : Exception
{
    public AttachmentRejectedException(AttachmentRejectionReason reason, string fileName, string message)
        : base(message)
    {
        Reason = reason;
        FileName = fileName;
    }

    public AttachmentRejectionReason Reason { get; }

    /// <summary>Izvorni naziv odbijene datoteke.</summary>
    public string FileName { get; }
}
