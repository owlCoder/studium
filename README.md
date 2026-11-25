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

### Početna strana sa karticama predmeta (ST-007, US-7.7)

`GET /` otvara početnu stranu (`src/Api/wwwroot`). Student posle unosa tokena sesije vidi kartice svojih predmeta (naziv i akademska godina) iz zaštićenog `GET /pocetna`. U Development okruženju API pri pokretanju ispisuje demo token studenta. Detalji, dijagram toka i testovi: [docs/2/us-7.7-kartice-izvodjenja.md](docs/2/us-7.7-kartice-izvodjenja.md), odluka: [ADR-0004](docs/adr/0004-web-prikaz-pocetne-strane.md).

### Registar podržanih funkcija (ST-007, US-7.6)

Sistem vodi listu podržanih vrsta aktivnosti (obaveštenje, materijal, zadatak) i funkcionalnih oblasti, kako početni pregled ne bi prikazivao veze ka funkcijama koje ne rade. Lista je u `src/Infrastructure/Dashboard/SupportedFeatures.cs`, a pravila u `src/Domain/Dashboard/FeatureRegistry.cs`. Nepodržana vrsta aktivnosti se ne broji u otvorenim aktivnostima (US-7.3) i nema vezu na početnoj strani (US-7.4, US-7.10). `GET /features` vraća podržane vrste i oblasti, a `GET /features/activity-types/{vrsta}` proverava da li je vrsta podržana; isti spisak ispisuje i konzolna aplikacija (`dotnet run --project src/ConsoleUi -- st-007`). Testovi: `tests/Tests/Domain/Dashboard`, `tests/Tests/Application/Dashboard`, `tests/Tests/Infrastructure/Dashboard`.

### Testovi prihvatanja početnog pregleda (ST-007, US-7.12)

Automatski testovi prihvatanja (`tests/Tests/Acceptance`) proveravaju ceo početni pregled nad pravim komponentama US-7.1–7.10 i kroz zaštićeni `GET /pocetna`: student iz dva predmeta sa jednim otvorenim zadatkom vidi oba predmeta i vezu ka zadatku, promena roka se vidi već pri sledećem zahtevu, odgovor se ne kešira i meri se vreme odgovora za korisnika sa 10 izvođenja. Podaci u testovima su sintetički. Pokretanje samo testova prihvatanja:

    dotnet test Solution.sln --filter "TestCategory=Acceptance" --logger "console;verbosity=detailed"

Rezultati izvršavanja i merenja: [docs/2/us-7.12-testovi-prihvatanja.md](docs/2/us-7.12-testovi-prihvatanja.md).

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

## KT2 scenario (ST-011 / US-01)

U prvom PowerShell terminalu, iz korena repozitorijuma, pokreni API i ostavi ga da radi:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Api --configuration Release -- --urls http://127.0.0.1:5098
```

U drugom terminalu pokreni konzolnu formu:

```powershell
dotnet run --project src/ConsoleUi --configuration Release -- st-011
```

1. Unesi prikazani ID izvođenja ERS. Opcijama `1`–`4` unesi naziv `Domaći zadatak`, opis, uputstvo i rok `15.01.2027 18:30 +01:00`.
2. Ponovo izaberi `1` i promeni naziv u `Domaći zadatak 1`. Proveri da se izmenjeni naziv i ostale vrednosti vide u formi.
3. Izaberi `5` za čuvanje. Konzola ispisuje `Nacrt je sačuvan. ID zadatka:` i identifikator koji je vratio API.
4. Ponovo pokreni konzolnu formu, izaberi prikazani ID izvođenja OIB, unesi naziv i potvrdi opcijom `5`. Demo nastavnik je angažovan na ERS-u, pa za OIB API vraća `403 Forbidden`, a konzola prikazuje poruku o nedozvoljenom pristupu.

Za proveru grešaka probaj da potvrdiš prazan naziv ili da posle važećeg roka uneseš `31.02.2027 18:30 +01:00`. Forma ostaje otvorena i možeš da nastaviš unos. Opcija `0` prekida unos bez slanja zahteva.

Scenario koristi demo prijavu i memorijsko čuvanje. Nacrti se gube kad se API zaustavi. [Dijagrami arhitekture i toka podataka za US-01](docs/tim14/st-011-us-01-kt2.md)

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
