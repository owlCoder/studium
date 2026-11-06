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

## Podaci, identitet, veze i transakcione granice

- **Vlasništvo nad podacima:** modul `Application.Activities` poseduje slučajeve upotrebe
  aktivnosti i izlazni model; `Domain.Activity` čuva invarijante aktivnosti. Katalog
  predmeta ostaje u vlasništvu modula `Courses` i ne duplira se u ovom modulu.
- **Identitet entiteta:** aktivnost ima stabilan `Guid Id` koji dodeljuje
  `CreateActivityHandler`; naziv nije identifikator. `CourseId` je obavezan i mora
  pokazivati na postojeći predmet u trenutku kreiranja.
- **Veze:** jedna aktivnost pripada tačno jednom predmetu preko `CourseId`
  (veza `Course 1 — * Activity`). Model namerno ne uvodi kopiju podataka predmeta.
- **Transakcione granice:** upis jedne aktivnosti je jedna atomska operacija
  `IActivityRepository.AddAsync`; memorijski adapter je sprovodi preko
  `ConcurrentDictionary.TryAdd`, pa duplikat ne menja prethodno stanje. Provera
  postojanja predmeta je read-only i prethodi upisu; budući trajni adapter mora da
  obezbedi da neuspešan upis ne ostavi delimično stanje i da razjasni ponašanje ako
  se predmet obriše između provere i upisa.
- **Način testiranja:** invarijante se testiraju u `ActivityTests`, ishodi slučajeva
  upotrebe u `CreateActivityHandlerTests` i `ListActivitiesHandlerTests`, a ponašanje
  adaptera (uključujući duplikat, otkazivanje i paralelne upise) u
  `InMemoryActivityRepositoryTests`. Trajni adapter treba da prođe isti ugovorni skup
  testova pre zamene registracije u composition root-u.
- **Otvoreno pitanje:** projektna specifikacija razlikuje kataloški predmet od izvođenja
  predmeta u akademskoj godini. Ovaj demonstracioni model veže aktivnost samo za
  `CourseId`; ako ST-021 ili povezani zahtevi budu zahtevali izvođenje i akademsku
  godinu, model i port se moraju revidirati pre trajnog skladištenja.

## Posledice

- Podaci se gube pri ponovnom pokretanju procesa. Adapter nije pogodan za stvarne podatke.
- Zamena trajnim adapterom zahteva samo novu implementaciju `IActivityRepository` i
  izmenu composition root-a; `Domain` i `Application` ostaju nepromenjeni.
- Port trenutno nema paginaciju, filtriranje po predmetu, optimističku konkurentnost
  niti transakcije. To treba rešiti zajedno sa izborom trajnog skladišta.
- Nema autentifikacije ni provere prava nad konkretnim izvođenjem predmeta, pa se
  endpointi ne smeju izlagati kao produkciona funkcionalnost.
- Model sa `CourseId` je dovoljan samo za demonstraciju; odluka o vezi sa izvođenjem
  predmeta i akademskom godinom ostaje otvorena za tim i specifikaciju.

## Povezanost sa specifikacijom

- **Pogođeni zahtevi:** grana nosi oznaku ST-021; ovaj ADR opisuje samo demonstracioni
  tehnički tok i ne tvrdi da je ST-021 implementiran.
- **Promena javnih ugovora i zavisnosti:** novi port `IActivityRepository` i novi HTTP
  endpointi; postojeći ugovori kataloga predmeta se ne menjaju.
- **Uticaj na čuvanje i migraciju podataka:** nema migracija; memorijsko stanje. Pre
  uvođenja trajnog skladišta potrebno je potvrditi da li se aktivnost vezuje za predmet
  ili za konkretno izvođenje u akademskoj godini.
- **Testovi koji proveravaju odluku:** `ActivityTests`, `CreateActivityHandlerTests`,
  `ListActivitiesHandlerTests`, `InMemoryActivityRepositoryTests`.

## Razmotrene alternative

1. **Direktno korišćenje `DbContext`-a u `Api` sloju** — odbijeno jer bi poslovna logika
   zavisila od tehnologije skladištenja i otežala zamenu adaptera.
2. **Referenca na `Course` unutar `Activity`** — odbijeno jer bi model aktivnosti zavodio
   od načina učitavanja drugog agregata; `CourseId` je dovoljan i pogodan za trajnu bazu.
3. **Čuvanje u datoteci ili lokalnoj bazi** — odbijeno jer zadatak traži jednostavan
   memorijski adapter koji se kasnije menja bez izmene domena i aplikacionog sloja.
