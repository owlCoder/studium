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



# STUDIUM: ST-005 Odgovorni nastavnik

Dio sistema STUDIUM (ST-001 do ST-100) koji pokriva dodjelu odgovornog
nastavnika i upravljanje saradnicima na izvođenju predmeta.

## Šta radi ovaj dio
- Administrator dodjeljuje odgovornog nastavnika izvođenju.
- Odgovorni nastavnik dodaje i uklanja nastavnike i asistente na svom izvođenju.
- Odgovorni nastavnik pregleda spisak saradnika.

## Poslovna pravila
- Aktivno izvođenje mora imati najmanje jednog odgovornog nastavnika.
- Odgovorni nastavnik ne može ukloniti sebe dok nije određen drugi.
- Uklanjanje saradnika ukida buduće operacije, ali čuva autorstvo ranijih zapisa.
- Za saradnika se koristi postojeći aktivan nalog odgovarajuće uloge.
- Ista osoba ne može biti dvaput angažovana na istom izvođenju.

## Pokretanje
1. Preduslovi: (npr. JDK 21, Node 20, PostgreSQL 16)
2. Kloniranje: git clone (URL repozitorija)
3. Konfiguracija baze: (naziv fajla / varijable okruženja)
4. Pokretanje: (komanda za pokretanje)
5. Testovi: (komanda za testove)

## Dijagrami
- Dijagram arhitekture: ST-005 Odgovorni nastavnik (docs/arhitektura-st005.png)
- Dijagram toka podataka (DFD): ST-005 Odgovorni nastavnik (docs/dfd-st005.png)

## Zavisnosti od drugih dijelova
- ST-001: korisnički nalozi
- ST-002: prijava i provjera ovlaštenja
- ST-004: izvođenja predmeta
- ST-089: evidencija promjena (planirano)

## Stanje implementacije (KT1)
Urađeno:
- (npr. entitet TeachingAssignment, migracija, unique indeks)
- (npr. dodjela odgovornog nastavnika)

Preostalo do KT2:
- (npr. dodavanje i uklanjanje saradnika, pregled spiska)
- (npr. testovi, integracija sa ST-002/004)

## Demo scenario
1. Administrator dodjeljuje Petra kao odgovornog nastavnika na ERS.
2. Petar dodaje asistenta Anu.
3. Petar pokuša ukloniti sebe: operacija se odbija.
4. Petar pregleda spisak saradnika.