# STUDIUM

**STUDIUM** je studentski projekat iz predmeta **Elementi razvoja softvera (ERS)**, Fakultet tehničkih nauka, Univerzitet u Novom Sadu. Cilj je razvoj informacionog sistema za podršku nastavi i studentskom radu.

Polazni dokument projekta je specifikacija sa funkcionalnim zahtevima **ST-001–ST-100**. Ona definiše očekivano ponašanje, poslovna pravila i primere. Kod iz ovog repozitorijuma **nije kompletna implementacija** specifikacije.

## Šta je pripremljeno

Početni šablon, napravljen po uzoru na nastavni primer Logger–Blogger, sadrži:

| Projekat | Odgovornost |
| --- | --- |
| `Domain` | Domenski entiteti i invarijante; ne zavisi od drugih projektnih slojeva |
| `Application` | Slučajevi upotrebe, portovi (interfejsi) i izlazni modeli |
| `Infrastructure` | Tehničke implementacije portova; na početku memorijski adapter |
| `Api` | Minimalni HTTP ulaz i composition root |
| `ConsoleUi` | Jednostavan konzolni ulaz za demonstraciju istog slučaja upotrebe |
| `Tests` | NUnit/Moq testovi domena, aplikacionog sloja i adaptera |

Priloženi **demo katalog predmeta** prikazuje tok `API/ConsoleUi → Application → Domain` i korišćenje interfejsa umesto zavisnosti od konkretne infrastrukture. Endpoint `/demo/courses` i demo podaci **nisu kompletna implementacija ST-003**: nema čuvanja u bazi, uređivanja predmeta, prijavljivanja korisnika niti provere ovlašćenja.

### Delimična ST-003 pretraga (grana tima 9)

`GET /courses?query=ers` pretražuje postojeći demonstracioni katalog po oznaci i nazivu predmeta (bez razlike između velikih i malih slova) i vraća `Id`, `Code` i `Name`. Prazan ili izostavljen `query` vraća sve demonstracione predmete, sortirane po oznaci.

**Ograničenje:** i ovaj endpoint trenutno čita isključivo memorijske demo podatke (`DemoData.CreateCourseReader()`), bez trajne baze, autentifikacije, autorizacije, unosa/izmene/brisanja predmeta i provere jedinstvenosti. Ne tretirati ga kao završen ST-003 niti koristiti za stvarne podatke. Za integraciju potpunog ST-003 potrebni su zasebni zahtevi, validacija i testovi prema projektnoj specifikaciji.

## Preduslovi

- .NET **8 SDK** (`dotnet --version`)
- Git
- Visual Studio / Visual Studio Code / Rider ili terminal

## Prvo pokretanje

```bash
git clone https://github.com/owlCoder/studium.git
cd studium
dotnet restore Solution.sln
dotnet build Solution.sln --configuration Release
dotnet test Solution.sln --configuration Release
dotnet run --project src/ConsoleUi
dotnet run --project src/Api
```

Poslednje dve komande pokreću alternativne ulaze; nisu potrebne istovremeno. API ispisuje adresu u terminalu. Proveriti `GET /health` i demonstracioni `GET /demo/courses`.

## Kako nastaviti

1. Pročitati [specifikaciju i mapiranje zahteva](docs/requirements/README.md) i odabrati dodeljeni zahtev ST-xxx.
2. Opisati poslovna pravila i kriterijume prihvatanja pre pisanja implementacije.
3. Odrediti granice: model u `Domain`, orkestracija u `Application`, implementacija zavisnosti u `Infrastructure`, ulaz u `Api` ili `ConsoleUi`.
4. Dodati testove za uspešne, granične i odbijene ishode; proveriti prava pristupa na nivou zapisa.
5. Raditi na grani `feature/<st-oznaka>-<opis>` i otvoriti Pull Request.
6. Za bitne projektantske odluke koristiti [ADR šablon](docs/templates/adr.md).

**Bezbednost:** početni šablon ne sadrži autentifikaciju, trajnu bazu niti produkcionu konfiguraciju. Ne izlagati ga internetu kao gotovu aplikaciju. Ne koristiti demonstracione vrednosti kao produkcione kredencijale.

## Dokumentacija

- [Uputstvo za doprinos](CONTRIBUTING.md)
- [Arhitektonske granice](docs/architecture.md)
- [ST-001–ST-100: indeks svih zahteva](docs/requirements/ST-INDEX.md)
- [Praćenje zahteva i kriterijumi prihvatanja](docs/requirements/README.md)
- [Vodič za prvi doprinos](docs/first-contribution.md)
- [Smernice za model podataka](docs/data-model-guidelines.md)
- [Predlog razvojnog toka prema oblastima ERS](docs/learning-path.md)
- [Pravila za AI agente](AGENTS.md)
- [ADR šablon](docs/templates/adr.md)

Materijali predmeta i detaljna projektna specifikacija predstavljaju izvor poslovnih zahteva; primeri u repozitorijumu služe isključivo za početnu organizaciju koda.

## ST-004: akademske godine i izvođenja predmeta (tim 5)

Delimična in-memory implementacija. Nije kompletan ST-004 (videti *Ograničenja*).

### Pokretanje

```bash
dotnet restore PROJEKAT-ERS.sln
dotnet build PROJEKAT-ERS.sln --configuration Release
dotnet test PROJEKAT-ERS.sln --configuration Release

dotnet run --project src/ConsoleUi                  # katalog + spisak demo scenarija
dotnet run --project src/ConsoleUi -- --demo-st004  # godina, izvođenje i prelazi stanja
dotnet run --project src/ConsoleUi -- --demo-godine # akademske godine
dotnet run --project src/ConsoleUi -- --sve         # oba scenarija
```

U Visual Studio-u: desni klik na `ConsoleUi` → *Properties* → *Debug* → *Command line arguments* (npr. `--demo-st004`); testovi preko *Test → Run All Tests*.

### Urađeno

**KT1**
- Domain: `AcademicYear`, `CourseOffering`, `OfferingState`
- Application: kreiranje, pregled, pretraga i izmena akademskih godina; kreiranje izvođenja
- Infrastructure: `InMemoryAcademicYearRepository`, `InMemoryCourseOfferingRepository` (atomarno dodavanje uz `lock`)
- Dijagrami arhitekture i toka podataka u `docs/5/`

**KT2**
- `ChangeOfferingStateHandler`: promena stanja izvođenja (U pripremi → Aktivno → Završeno)
- `ListCourseOfferingsHandler`: pregled izvođenja po akademskoj godini, uz filter po stanju
- `ICourseOfferingRepository` proširen metodama `ListByAcademicYearAsync` i `TryUpdateAsync`
- ConsoleUi: demo scenariji se biraju argumentom; neočekivana greška vraća izlazni kod 1
- Testovi: `OfferingStateTests` (prelazi, odbijeni prelazi, pregled) i `St004DemoSmokeTests` (ceo demo)

### Poslovna pravila

- Oznaka akademske godine je obavezna, bez razmaka na krajevima i jedinstvena bez obzira na velika i mala slova.
- Početak godine prethodi završetku; izmena zadržava isti Id.
- Izvođenje je jedinstveno za par (predmet, akademska godina); deaktiviran predmet ne može dobiti novo izvođenje.
- Stanja izvođenja idu samo unapred: U pripremi → Aktivno → Završeno. Preskakanje i vraćanje unazad su zabranjeni.
- Odbijena operacija ne menja prethodno važeće stanje.

### Integracija sa drugim delovima

| Deo | Smer | Interfejs | Status |
| --- | --- | --- | --- |
| ST-003 katalog predmeta | ST-004 koristi | `ICourseReader` (postojanje i `IsActive` predmeta) | povezano |
| ST-002 prijava i uloge | ST-004 koristi | interfejs tima ST-002 za trenutnog korisnika i ulogu | u toku |
| ST-006 upis studenata | ST-006 koristi ST-004 | `CourseOffering.Id` i stanje izvođenja | u toku |
| ST-005 nastavnici | ST-005 koristi ST-004 | `CourseOffering.Id` | u toku |

### Ograničenja

- Nema provere uloge Administrator dok se ne poveže interfejs ST-002.
- Podaci se čuvaju samo u memoriji; `lock` važi unutar jednog procesa. Trajna baza mora imati unique ograničenje nad oznakom godine i nad parom (CourseId, AcademicYearId).
- Svojstvo `IsArchived` postoji, ali arhiviranje (ST-091) nije implementirano.

### Dijagrami

- [Arhitektura](docs/5/st-004-akademske-godine-arhitektura.png) ([izvor](docs/5/st-004-akademske-godine-arhitektura.drawio))
- [Tok podataka (DFD)](docs/5/st-004-akademske-godine-dfd.png) ([izvor](docs/5/st-004-akademske-godine-dfd.drawio))
