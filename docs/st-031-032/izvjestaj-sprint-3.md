# Izveštaj sprinta 3: ST-031 i ST-032

Sprint 3 traje od 19. do 26.11.2026.

## ST-031: testovi prihvatanja

Primer iz specifikacije: „Vežbe za V1 traju od 10:00 do 11:30. Završetak u 09:30 se odbija. Otkazivanjem termin ostaje u istoriji, ali se više ne prikazuje kao aktivna obaveza i ne dozvoljava nov unos prisustva.”

Testovi su u `tests/Tests/Acceptance/St031AcceptanceTests.cs`.

| Test | Šta proverava | Rezultat |
|---|---|---|
| `VezbeV1_From1000To1130_AreAcceptedAsPlanned` | Vežbe od 10:00 do 11:30 se prihvataju kao Planiran i pronalaze se pretragom | |
| `VezbeV1_EndingAt0930_AreRejected` | Završetak u 09:30 se odbija i ništa se ne čuva | |
| `CancelledSession_StaysInHistory_AndRejectsNewAttendance` | Otkazan termin je u istoriji, nije među aktivnima i ne prima unos prisustva | |

Komanda kojom su testovi pokrenuti:

```bash
dotnet test Solution.sln --filter "FullyQualifiedName~St031AcceptanceTests"
```

**Ukupan rezultat:** svi testovi su uspjesno prosli
