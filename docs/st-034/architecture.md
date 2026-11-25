# ST-034 Pregled prisustva studenta – arhitektura

Dijagram prikazuje komponente ST-034, njihove odgovornosti, interfejse (portove) i veze sa ostatkom sistema STUDIUM.

```mermaid
flowchart TB
  subgraph API["① Api – ulaz"]
    direction LR
    E1["GET /offerings/{id}/attendance/me<br/>student"]
    E2["GET /offerings/{id}/attendance?groupId&from&to&kind<br/>nastavnik"]
  end

  subgraph APP["② Application – ST-034"]
    direction LR
    H1["GetMyAttendanceHandler<br/><i>lični pregled (US1, US3, US4)</i>"]
    H2["GetGroupAttendanceHandler<br/><i>pregled grupe (US2, US4)</i>"]
    DTO["AttendanceOverviewDto<br/><i>izlazni model</i>"]
  end

  subgraph DOM["③ Domain – ST-034"]
    S["AttendanceSummary<br/><i>broji Prisutan / Odsutan / Neevidentirano<br/>% = P / (P + O) × 100  ili  'Nema evidencije'</i>"]
  end

  subgraph PORTS["④ Portovi – interfejsi koje ST-034 koristi"]
    direction LR
    P1["ICurrentUser<br/><i>ko je prijavljen → ST-002</i>"]
    P2["IOfferingAccess<br/><i>član / angažovan? → ST-005, ST-006</i>"]
    P3["ITeachingSessionReader<br/><i>održani termini → ST-031</i>"]
    P4["IAttendanceRecordReader<br/><i>zapisi prisustva → ST-032, ST-033</i>"]
    P5["IEligibilityProvider<br/><i>ko je bio u grupi → ST-006, ST-019</i>"]
  end

  subgraph OTHER["⑤ Ostatak sistema STUDIUM – drugi timovi"]
    direction LR
    O1["ST-002<br/>Prijava"]
    O2["ST-005 / ST-006<br/>Angažovanje, članstvo"]
    O3["ST-031<br/>Termini nastave"]
    O4["ST-032 / ST-033<br/>Prisustvo, ispravke"]
    O5["ST-019<br/>Grupe"]
  end

  INF["Infrastructure<br/><i>memorijski adapteri za testiranje</i>"]

  E1 --> H1
  E2 --> H2
  APP --> DOM
  APP --> PORTS
  PORTS -. "implementiraju drugi timovi" .-> OTHER
  INF -. implementira .-> PORTS

  classDef mine fill:#e6f4f1,stroke:#0f766e,stroke-width:2px;
  classDef other fill:#f3f4f6,stroke:#9ca3af;
  class H1,H2,DTO,S,E1,E2 mine;
  class O1,O2,O3,O4,O5 other;
```

## Komponente i odgovornosti

| Komponenta | Sloj | Odgovornost |
| --- | --- | --- |
| `GET /offerings/{id}/attendance/me` | Api | Ulaz za studenta; identitet se uzima iz prijave |
| `GET /offerings/{id}/attendance` | Api | Ulaz za nastavnika; filteri grupa, period, vrsta nastave |
| `GetMyAttendanceHandler` | Application | Lični pregled: provera članstva, izbor podobnih održanih termina, sastavljanje pregleda |
| `GetGroupAttendanceHandler` | Application | Pregled grupe: provera angažovanja, filteri, pregled po studentu |
| `AttendanceOverviewDto` | Application | Izlazni model: termini sa stanjem, brojevi, procenat ili „Nema evidencije“ |
| `AttendanceSummary` | Domain | Brojanje stanja i računanje procenta P / (P + O) × 100, dve decimale, polovina naviše |

## Portovi i vlasnici

| Port | Šta daje | Izvor (drugi tim) |
| --- | --- | --- |
| `ICurrentUser` | Identitet prijavljenog korisnika | ST-002 |
| `IOfferingAccess` | Da li je korisnik član ili angažovan na izvođenju | ST-005, ST-006 |
| `ITeachingSessionReader` | Održani i otkazani termini izvođenja | ST-031 (`TeachingSession`) |
| `IAttendanceRecordReader` | Zapisi prisustva po terminu | ST-032, ST-033 (`AttendanceRecord`) |
| `IEligibilityProvider` | Podobni studenti za termin u vreme održavanja | ST-006, ST-019 |

Dok drugi timovi ne završe svoje delove, portove implementiraju memorijski adapteri u `Infrastructure`.
