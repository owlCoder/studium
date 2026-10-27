# Doprinos projektu STUDIUM

## Razvojni tok

1. Izabrati zadatak koji potiče iz specifikacije (`ST-xxx`) i napisati kriterijume prihvatanja.
2. Sinhronizovati lokalni `main` i kreirati granu, npr. `feature/st-011-zadaci`.
3. Praviti male, tematski povezane commit-ove. Ne menjati nepovezane celine usput.
4. Dodati/izmeniti relevantne testove i izvršiti `dotnet build` i `dotnet test`.
5. Otvoriti Pull Request prema `main`, povezati ST oznaku i radni zadatak.
6. Priložiti opis poslovnih pravila, izmenjenih ugovora i izvršenih provera.
7. Po rešavanju komentara sa pregleda, ovlašćeni član integriše promenu.

## Arhitektura i integracija

- `Domain` ne zavisi od API-ja, baze ili prezentacije.
- `Application` zavisi od `Domain` i koristi sopstvene portove za spoljne usluge.
- `Infrastructure` implementira portove; `Api` i `ConsoleUi` povezuju zavisnosti.
- Svaki zahtev koristi kontekst **konkretnog izvođenja predmeta i akademske godine** kada se to zahteva specifikacijom. Katalog predmeta nije isto što i izvodjenje predmeta.
- Proveriti identitet, članstvo i ovlašćenje nad **konkretnim zapisom**; skrivanje dugmeta u UI nije bezbednosna kontrola.
- Ne dodavati duplirane evidencije tamo gde se prikaz može izvesti iz izvornog stanja.
- Promene zajedničkih ugovora i modela unapred usaglasiti sa pogođenim timovima.

## Kvalitet

- Testirati i neuspešne ishode: nevažeći ulaz, duplikat, nedozvoljen pristup, neuspelu promenu i konkurentan zahtev tamo gde je važno.
- Ne smatrati 100% pokrivenost koda dokazom ispravnosti.
- U PR-u navesti šta nije obuhvaćeno, a ne predstavljati delimičan demo kao realizovan zahtev.
- Značajne odluke dokumentovati kroz `docs/adr/` korišćenjem [šablona](docs/templates/adr.md).

## Poverljivi podaci

Nikada ne unositi lozinke, pristupne tokene, privatne ključeve, stvarne studentske podatke i produkcione sertifikate u repozitorijum. Koristiti anonimne ili sintetičke testne podatke. Ako tajna dospe u Git istoriju, odmah je opozvati i prijaviti nastavnom timu; samo brisanje sledećim commit-om nije dovoljno.
