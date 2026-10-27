# STUDIUM

**STUDIUM** je studentski projekat iz predmeta **Elementi razvoja softvera (ERS)**, Fakultet tehničkih nauka, Univerzitet u Novom Sadu. Cilj je razvoj informacionog sistema za podršku nastavi i studentskom radu.

Polazni dokument projekta je specifikacija sa funkcionalnim zahtevima **ST-001–ST-100**. Ona definiše očekivano ponašanje, poslovna pravila i primere. Kod iz ovog repozitorijuma **nije kompletna implementacija** specifikacije.

## Šta je pripremljeno

Početni šablon, napravljen po uzoru na nastavni primer Logger–Blogger, sadrži:

| Projekat | Odgovornost |
| --- | --- |
| `Studium.Domain` | Domenski entiteti i invarijante; ne zavisi od drugih projektnih slojeva |
| `Studium.Application` | Slučajevi upotrebe, portovi (interfejsi) i izlazni modeli |
| `Studium.Infrastructure` | Tehničke implementacije portova; na početku memorijski adapter |
| `Studium.Api` | Minimalni HTTP ulaz i composition root |
| `Studium.ConsoleUi` | Jednostavan konzolni ulaz za demonstraciju istog slučaja upotrebe |
| `Studium.Tests` | NUnit/Moq testovi domena, aplikacionog sloja i adaptera |

Priloženi **demo katalog predmeta** prikazuje tok `API/ConsoleUi → Application → Domain` i korišćenje interfejsa umesto zavisnosti od konkretne infrastrukture. Endpoint `/demo/courses` i demo podaci **nisu implementacija ST-003**: nema čuvanja u bazi, uređivanja predmeta, prijavljivanja korisnika niti provere ovlašćenja.

## Preduslovi

- .NET **8 SDK** (`dotnet --version`)
- Git
- Visual Studio / Visual Studio Code / Rider ili terminal

## Prvo pokretanje

```bash
git clone https://github.com/owlCoder/studium.git
cd studium
dotnet restore Studium.sln
dotnet build Studium.sln --configuration Release
dotnet test Studium.sln --configuration Release
dotnet run --project src/Studium.ConsoleUi
dotnet run --project src/Studium.Api
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
