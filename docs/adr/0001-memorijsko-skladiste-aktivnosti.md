# ADR-0001: Memorijsko skladište aktivnosti predmeta

- **Status:** Predložen
- **Datum:** 2026-11-06
- **Vlasnici:** Tim za granu ST-021 (aktivnosti)
- **Pogođene projektne celine:** `Domain.Activity`, `Application.Activities`, `Infrastructure.Activities`, `Api`

## Kontekst

Projektni kostur ima demonstracioni tok za katalog predmeta preko porta `ICourseReader`.
Za razvoj i testiranje potreban je sličan, ograničen tok za aktivnosti predmeta:
dodavanje aktivnosti postojećem predmetu i prikaz svih aktivnosti. Trajna baza,
migracije i autentifikacija nisu deo ovog zadatka i ne smeju se pretpostaviti.

## Odluka

- Domenski tip `Domain.Activity.Activity` čuva `CourseId`, a ne referencu na ceo agregat
  predmeta. Time domen ostaje nezavisan od načina učitavanja predmeta.
- `Application.Activities` definiše port `IActivityRepository` sa operacijama
  `AddAsync` i `ListAsync`, kao i slučajeve upotrebe `CreateActivityHandler` i
  `ListActivitiesHandler`.
- `Infrastructure.Activities.InMemoryActivityRepository` je prva implementacija porta.
  Koristi `ConcurrentDictionary` da bi istovremeni zahtevi bili bezbedni i da se
  duplikat identifikatora odbije bez delimične promene stanja.
- `Api` registruje adapter kao singleton, jer se memorijsko stanje mora zadržati između
  HTTP zahteva, i mapira ishode slučaja upotrebe u `201`, `400` i `404`.
- Provera postojanja predmeta koristi postojeći port `ICourseReader`; logika predmeta
  se ne menja.

## Posledice

- Podaci se gube pri ponovnom pokretanju procesa. Adapter nije pogodan za stvarne podatke.
- Zamena trajnim adapterom zahteva samo novu implementaciju `IActivityRepository` i
  izmenu composition root-a; `Domain` i `Application` ostaju nepromenjeni.
- Port trenutno nema paginaciju, filtriranje po predmetu, optimističku konkurentnost
  niti transakcije. To treba rešiti zajedno sa izborom trajnog skladišta.
- Nema autentifikacije ni provere prava nad konkretnim izvođenjem predmeta, pa se
  endpointi ne smeju izlagati kao produkciona funkcionalnost.

## Povezanost sa specifikacijom

- **Pogođeni zahtevi:** grana nosi oznaku ST-021; ovaj ADR opisuje samo demonstracioni
  tehnički tok i ne tvrdi da je ST-021 implementiran.
- **Promena javnih ugovora i zavisnosti:** novi port `IActivityRepository` i novi HTTP
  endpointi; postojeći ugovori kataloga predmeta se ne menjaju.
- **Uticaj na čuvanje i migraciju podataka:** nema migracija; memorijsko stanje.
- **Testovi koji proveravaju odluku:** `ActivityTests`, `CreateActivityHandlerTests`,
  `ListActivitiesHandlerTests`, `InMemoryActivityRepositoryTests`.

## Razmotrene alternative

1. **Direktno korišćenje `DbContext`-a u `Api` sloju** — odbijeno jer bi poslovna logika
   zavisila od tehnologije skladištenja i otežala zamenu adaptera.
2. **Referenca na `Course` unutar `Activity`** — odbijeno jer bi model aktivnosti zavodio
   od načina učitavanja drugog agregata; `CourseId` je dovoljan i pogodan za trajnu bazu.
3. **Čuvanje u datoteci ili lokalnoj bazi** — odbijeno jer zadatak traži jednostavan
   memorijski adapter koji se kasnije menja bez izmene domena i aplikacionog sloja.
