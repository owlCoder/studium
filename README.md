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

### Delimična evidencija aktivnosti predmeta (grana ST-021)

- `POST /Course/{contextSlug}/CreateActivity` dodaje aktivnost kontekstu koji razrešava port `IActivityContextReader` i vraća `201 Created`. Nepostojeći kontekst daje `404`, a nevažeći naziv ili interval daje `400` bez promene stanja. Dupli identifikator aktivnosti vraća `409`.

Domen i memorijski adapter ne bacaju izuzetke za neispravne podatke ni za otkazane zahteve: `Activity.TryCreate` vraća `ActivityCreation`, a port `IActivityRepository` vraća `ActivityAddResult` odnosno `ActivityListResult` sa eksplicitnim statusom (`Added`, `DuplicateId`, `InvalidActivity`, `Canceled`). Otkazan zahtev se prema klijentu prijavljuje statusom `499`, a ne izuzetkom.

`contextSlug` je kratka oznaka oblika `OZNAKA-xxxxx`, gde je `OZNAKA` oznaka predmeta, a `xxxxx` poslednjih pet znakova njegovog identifikatora (bez razlike između velikih i malih slova). Isti obrazac važi i za buduće rute pojedinačne aktivnosti (`/Course/{contextSlug}/Activity/{activitySlug}`, gde je `activitySlug` oblika `NAZIV-xxxxx`).

**Kontekst aktivnosti:** sloj `Domain` i `IActivityRepository` ne znaju šta oznaka predstavlja i vide samo neprozirni `ContextId`. Oznaku iz putanje razrešava isključivo `IActivityContextReader`; trenutna implementacija `InMemoryActivityContextReader` je omotač oko `ICourseReader` i vraća `ActivityContext` za kataloški predmet. Kada ST-004 uvede izvođenja predmeta, dovoljno je dodati novi adapter tog porta (npr. `OfferingActivityContextReader`) i promeniti jednu liniju registracije u `Program.cs`; ruta, rukovalac, domen i repozitorijum ostaju nepromenjeni. Port vraća `ActivityContextLookup` sa statusima `Found`, `NotFound`, `Ambiguous` i `Canceled` — bez izuzetaka.

Oznaka se normalizuje: zadržavaju se slova, cifre, `-`, `_` i `.`, svaki drugi znak (razmak, `/` i sl.) zamenjuje se jednom crticom, a vodeće i završne crtice se uklanjaju. Ako oznaka nakon normalizacije ostane prazna, slug sadrži samo `xxxxx`.

Primer zahteva (koristi jedan od demo predmeta iz `DemoData`; `ERS` sa identifikatorom `11111111-1111-1111-1111-111111111111` daje `ERS-11111` kao oznaku konteksta):

```bash
curl -X POST http://localhost:5000/Course/ERS-11111/CreateActivity \
  -H "Content-Type: application/json" \
  -d '{"name":"Timski projekat","description":"Prva faza","startDate":"2026-03-01T08:00:00Z","endDate":"2026-03-15T23:59:00Z"}'
```

**Ograničenje:** aktivnosti se čuvaju samo u memoriji procesa preko porta `IActivityRepository` (`InMemoryActivityRepository`) i gube se pri ponovnom pokretanju. Nema trajne baze, autentifikacije, autorizacije ni provere prava nad konkretnim izvođenjem predmeta. Ovaj tok ne predstavlja završenu implementaciju ST-021 (formiranje studentskih timova) niti bilo kog drugog ST zahteva.

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
