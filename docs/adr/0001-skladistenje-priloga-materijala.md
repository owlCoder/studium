# ADR-0001: Skladištenje priloga nastavnih materijala na lokalnom disku

- **Status:** Predložen
- **Datum:** 2026-11-10
- **Vlasnici:** tim ST-009 (US-04)
- **Pogođene projektne celine:** ST-009

## Kontekst

ST-009 traži da nastavnik materijalu priloži datoteke koje student kasnije preuzima. Sadržaj datoteka mora biti negde fizički sačuvan. Poslovno pravilo: neispravan prenos ne sme ostaviti objavljen materijal sa nedostajućom datotekom. Baza podataka i storage servis još nisu uvedeni u projekat.

## Odluka

- U `Application` se uvodi port `IAttachmentFileStorage` (`SaveAsync`, `OpenReadAsync`).
- U `Infrastructure` se uvodi adapter `LocalDiskAttachmentFileStorage` koji čuva sadržaj u zadatom korenskom direktorijumu.
- Ključ datoteke generiše skladište (GUID, 32 heksadecimalna znaka). Izvorni naziv datoteke se **ne** koristi kao putanja; on se čuva kao metapodatak priloga (sledeća tačka US-04).
- Sadržaj se upisuje u privremenu datoteku i tek po uspešnom upisu premešta pod konačni ključ; pri grešci ili otkazivanju privremena datoteka se briše.
- `OpenReadAsync` prihvata samo ključ u formatu koji skladište generiše, čime se sprečava pristup putanjama van korenskog direktorijuma.

## Posledice

- Domen i slučajevi upotrebe ne zavise od načina skladištenja; disk se kasnije može zameniti storage servisom implementacijom istog porta.
- Skladište ne proverava prava pristupa — to je odgovornost slučajeva upotrebe (prilog nasleđuje pravo pristupa materijala, US-12).
- Lokalni disk nije pogodan za više instanci API-ja bez deljenog diska.
- Dodavanje operacija brisanja i provere postojanja ostavljeno je za US-07 i US-08.

## Povezanost sa specifikacijom

- **Pogođeni zahtevi:** ST-009
- **Promena javnih ugovora i zavisnosti:** novi port `IAttachmentFileStorage`; postojeći ugovori nisu menjani.
- **Uticaj na čuvanje i migraciju podataka:** nema migracija; korenski direktorijum se konfiguriše pri povezivanju zavisnosti u `Api`.
- **Testovi koji proveravaju odluku:** `tests/Tests/Infrastructure/LocalDiskAttachmentFileStorageTests.cs`.

## Razmotrene alternative

- **Sadržaj u bazi (BLOB):** baza još nije izabrana; veliki prilozi opterećuju bazu.
- **Eksterni storage servis (npr. S3/Azure Blob):** zahteva nalog i tajne ključeve, nepotrebno za nastavni projekat u ovoj fazi; ostaje moguć kroz isti port.
- **Izvorni naziv kao naziv datoteke na disku:** rizik od kolizija i path traversal napada.
