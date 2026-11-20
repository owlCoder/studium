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

## KT1 scenario (ST-031 i ST-032)

```bash
dotnet run --project src/ConsoleUi -- st-031-032
```

Komanda pokreće scenario kontrolne tačke: odbijen termin koji se završava pre početka, kreiranje termina, označavanje kao Održan, spisak podobnih studenata, unos prisustva i prikaz da student bez unosa ostaje `Neevidentirano` (ne `Odsutan`), kao i odbijanje unosa na otkazan termin.

### Provera oba scenarija

1. `dotnet run --project src/ConsoleUi` → prva linija je `STUDIUM | Demonstracija čitanja kataloga (nije ST-003)` i ispisuje listu predmeta.
2. `dotnet run --project src/ConsoleUi -- st-031-032` → prva linija je `STUDIUM | KT1 scenario (ST-031 + ST-032)`, a u koraku 6 Marko ima stanje `Neevidentirano`.

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

## ST-004: stanje i plan

### Urađeno
- Domain: AcademicYear, CourseOffering, OfferingState (validacije, stanja samo unapred)
- Application: handleri za kreiranje akademske godine i izvođenja
- Infrastructure: memorijski repozitorijumi; provera i upis su atomarni (TryAddAsync pod lock-om)
- Testovi: 8 NUnit testova, uključujući test sa 20 paralelnih pokušaja
- Dijagrami: docs/st-004-arhitektura.png, docs/st-004-dfd.png

### Poslovna pravila
- Početak godine pre kraja; oznaka godine jedinstvena (bez razlike u veličini slova)
- Par (predmet, godina) jedinstven; deaktiviran predmet nema novo izvođenje
- Stanja: U pripremi → Aktivno → Završeno

### Ograničenja (delimična implementacija)
- Ovo nije kompletan ST-004: nema provere uloge Administrator, Api/ConsoleUi ulaza ni trajne baze.
- Svojstvo IsArchived postoji, ali logika arhiviranja (ST-091) nije implementirana.
- `lock` važi samo unutar jednog procesa; trajna baza mora garantovati jedinstvenost unique ograničenjem
  nad parom (CourseId, AcademicYearId) i nad oznakom godine.

### Preostalo do KT2
- Handler za promenu stanja i pregled izvođenja
- Provera uloge Administrator (zavisi od ST-001/002)
- Povezivanje sa Api/ConsoleUi
- Više negativnih testova, ažuriranje dijagrama i README

### Zavisnosti
- ST-001/002: korisnici i uloge
- ST-003: Course, ICourseReader
- ST-005, ST-006: koriste CourseOffering.Id

## ST-004: akademske godine (grana `feature/st-004-akademske-godine-tim-5`)

Delimična in-memory/demonstraciona implementacija.

**Urađeno (KT1)**
- Application: `ListAcademicYearsHandler` (pregled i pretraga po oznaci), `UpdateAcademicYearCommand` i `UpdateAcademicYearHandler` (izmena); `IAcademicYearRepository` proširen metodama `ListAsync` i `TryUpdateAsync`
- Infrastructure: `ListAsync` i atomarni `TryUpdateAsync` u `InMemoryAcademicYearRepository`
- ConsoleUi: demo scenario `AcademicYearsDemo` (kreiranje, lista, pretraga, izmena i odbijeni unosi)
- Testovi: 10 NUnit testova u `AcademicYearsHandlersTests`
- Dijagrami: `docs/5/st004-akademske-godine-arhitektura.png`, `docs/5/st004-akademske-godine-dfd.png` (izvori u `.drawio`)

**Poslovna pravila**
- Oznaka je obavezna, bez razmaka na krajevima i jedinstvena bez razlike u veličini slova
- Početak godine prethodi završetku
- Izmena zadržava isti Id
- Odbijena operacija ne menja postojeće podatke

**Pokretanje demoa:** `dotnet run --project src/ConsoleUi -- st-004`

**Preostalo:** provera uloge Administratora, trajno skladište, veza sa izvođenjima.

## Pokretanje

1. Kloniraj repozitorijum i otvori solution u Visual Studiju.
2. Instaliraj .NET 8 SDK (`dotnet --list-sdks` treba da prikaže liniju koja počinje sa `8.0.`).
3. Desni klik na projekat `ConsoleUi`, pa **Set as Startup Project**.
4. Pokreni sa **Ctrl+F5**.

Očekivani ispis polaznog demoa: naslov "STUDIUM | Demonstracija čitanja kataloga"
i spisak predmeta (ERS, OIB). Program se završava sa kodom 0.

Testovi: **Test → Run All Tests** (NUnit, projekat `Tests`).

## Struktura rješenja

| Projekat | Uloga |
|---|---|
| `Domain` | Entiteti i pravila koja važe uvijek (validacija u konstruktoru) |
| `Application` | Servisi i interfejsi, provjera ovlašćenja |
| `Infrastructure` | Repozitorijumi i čuvanje podataka |
| `ConsoleUi` | Izvršni projekat, konzolni meniji |
| `Api` | Rezervisano za API sloj |
| `Tests` | NUnit testovi |

Svaka funkcionalna cjelina ima svoj folder u Domain, Application i Infrastructure
(npr. `Courses`). Novi kod ide u isti obrazac.

## Rad u Git-u

- Grane: `feature/st-XXX-opis-tim-5`, svaka osoba radi u svojoj grani.
- Ne commit-ovati direktno u `main`; za svaku izmjenu otvara se Pull Request.
- Prije rada: `git pull`.
- Ne commit-ovati lozinke ni `.env` fajlove.

## Tim 5: ST-004 i ST-005

Akademske godine i izvođenja predmeta (ST-004), te nastavnici i saradnici na
predmetu (ST-005).Plan, pravila i zavisnosti: [plan](docs/tim5/plan.md).
Dijagrami: [arhitektura](docs/tim5/dijagram-arhitekture.drawio.png), [tok podataka](docs/tim5/dijagram-toka-podataka.drawio.png).

