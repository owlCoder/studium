namespace Application.Activities;

/// <summary>
/// Kontekst kome aktivnost pripada. Danas je to kataloški predmet; kada izvođenja
/// predmeta (ST-004) budu dostupna, isti tip nosi identifikator izvođenja i tada
/// nije potrebna izmena domena, portova ni slučajeva upotrebe.
/// </summary>
public sealed record ActivityContext(Guid Id);
