# ADR-0004: Web prikaz početne strane kao statički fajlovi u Api projektu

- **Status:** Predložen
- **Datum:** 2026-11-21
- **Vlasnici:** tim ST-007 (US-7.7 Mateja; proširuju US-7.8–US-7.12)
- **Pogođene projektne celine:** `Api` (wwwroot, Program.cs), `Infrastructure/Demo`, `tests`

## Kontekst

Sprint 2 ST-007 („Prikaz i provere”) traži korisnički prikaz početne strane: kartice izvođenja (US-7.7), oznaku uloge (US-7.8), bedž otvorenih aktivnosti (US-7.9), veze (US-7.10) i stanja prikaza (US-7.11). Repozitorijum do sada nije imao frontend (`docs/architecture.md`, „Šta NIJE pripremljeno”). Podaci već postoje kroz zaštićeni `GET /pocetna` (US-7.4, US-7.5).

## Odluka

- Stranica se isporučuje kao statički fajlovi iz `src/Api/wwwroot` (`index.html`, `css/`, `js/`), preko `UseDefaultFiles` i `UseStaticFiles`.
- JavaScript je podeljen po odgovornosti, bez okvira i bez build koraka:
  - `offering-card.js`: komponenta kartice (samo prikaz jednog izvođenja);
  - `home-api.js`: klijent za `GET /pocetna` (samo HTTP i greške);
  - `pocetna.js`: povezuje stranicu i klijent; sekcije i kartice crta `renderHomeSections` iz `home-sections.js` (US-7.8).
- Stranica ne sadrži podatke. Kartice dobija isključivo iz `GET /pocetna`, sa tokenom sesije u zaglavlju `Authorization` (US-7.5). ID studenta se nikad ne šalje.
- Podaci se upisuju samo kroz `textContent`, nikad kroz `innerHTML`.
- Dok ST-002 ne uvede prijavu, u **Development** okruženju se pri pokretanju pravi sesija demo studenta (`DemoSession`). Token je slučajan, ispisuje se u log i ne čuva se u repozitorijumu.

## Posledice

- Nema novih NuGet ni npm paketa; CI (`dotnet test`) radi kao i do sada.
- Ostali storyji Sprinta 2 dodaju elemente u `.offering-card__body` i stanja u `pocetna.js`, bez menjanja ugovora `GET /pocetna`.
- Token se do ST-002 unosi ručno i čuva u `sessionStorage` (samo dok je kartica pregledača otvorena). Ovo nije produkciona prijava.
- Testovi:
  - `tests/Tests/Api/HomePageEndpointTests.cs`: stranica i fajlovi se serviraju, stranica nema podatke, `/pocetna` vraća polja koja kartica koristi, tuđi i prekinuti predmeti se ne vraćaju;
  - `tests/Web/*.test.mjs` (Node 22, `node --test "tests/Web/*.test.mjs"`): komponenta kartice, stranica i stilovi za uzak ekran, klijent bez pregledača.

## Povezanost sa specifikacijom

- **Pogođeni zahtevi:** ST-007 (US-7.7; osnova za US-7.8–US-7.12)
- **Promena javnih ugovora i zavisnosti:** nema; koristi postojeći `GET /pocetna` (`enrolled[].offeringId`, `courseName`, `academicYear`)
- **Uticaj na čuvanje i migraciju podataka:** nema
- **Testovi koji proveravaju odluku:** `HomePageEndpointTests`, `DemoSessionTests`, `tests/Web/offering-card.test.mjs`, `tests/Web/pocetna-page.test.mjs`, `tests/Web/home-api.test.mjs`

## Razmotrene alternative

- **Blazor ili Razor Pages:** novi model renderovanja i zavisnosti za ceo tim, a prikaz je jednostavan.
- **Zaseban SPA projekat (React, Vue):** zahteva npm, build korak i CORS; preveliko za Sprint 2.
- **Prikaz u ConsoleUi:** ne ispunjava zahtev za prikaz na uskom ekranu.
- **WPF (desktop aplikacija):** radi samo na Windowsu, traži instalaciju i ne radi na telefonu, pa ne ispunjava zahtev za uzak ekran; STUDIUM je web sistem.
- **Fiksni demo token u kodu:** odbijeno, jer bi demonstraciona vrednost bila u repozitorijumu (`CONTRIBUTING.md`, „Poverljivi podaci”).
