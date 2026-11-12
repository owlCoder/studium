namespace Application.Activities;

/// <summary>
/// Ishod razrešavanja oznake konteksta. Implementacije ne bacaju izuzetke;
/// neuspeh, dvosmislenost i otkazivanje vraćaju se kao status.
/// </summary>
public enum ActivityContextStatus
{
    Found,
    NotFound,
    Ambiguous,
    Canceled
}
