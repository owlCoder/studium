## ST-031: Testovi prihvatanja

Implementirana su tri testa prihvatanja na osnovu primera iz specifikacije.

| Test | Opis |
|---|---|
| `VezbeV1_From1000To1130_AreAcceptedAsPlanned` | Proverava da se vežbe V1 u terminu od 10:00 do 11:30 prihvataju sa statusom Planiran i da se mogu pronaći putem pretrage. |
| `VezbeV1_EndingAt0930_AreRejected` | Proverava da se termin koji se završava u 09:30 odbija i da nakon odbijanja ne dolazi do čuvanja podataka. |
| `CancelledSession_StaysInHistory_AndRejectsNewAttendance` | Proverava da otkazani termin ostaje u istoriji, nije prikazan među aktivnim obavezama i ne dozvoljava novi unos prisustva. |

### Primer iz specifikacije

„Vežbe za V1 traju od 10:00 do 11:30. Završetak u 09:30 se odbija. Otkazivanjem termin ostaje u istoriji, ali se više ne prikazuje kao aktivna obaveza i ne dozvoljava nov unos prisustva.”

### Pokretanje testova

```bash
dotnet test Solution.sln --filter "FullyQualifiedName~St031AcceptanceTests"
```

**Rezultat testiranja:**

// Ovde upisati broj uspešno izvršenih testova.
// Na primer: 3/3 testa uspešna.
// Ako postoje neuspešni testovi, navesti koji su i razlog neuspeha.