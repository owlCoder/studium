namespace Studium.Domain.ST016.ValueObjects;

using Studium.Domain.ST016.Enums;

public sealed record CalendarEvent(
    Guid IzvorId,
    string Naziv,
    DateTime Vreme,
    VrstaAktivnosti Vrsta,
    string UrlVeze
);