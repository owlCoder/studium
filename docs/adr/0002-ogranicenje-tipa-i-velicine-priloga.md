# ADR-0002: Ograničenje tipa i veličine priloga nastavnih materijala

- **Status:** Predložen
- **Datum:** 2026-11-11
- **Vlasnici:** tim ST-009 (US-05)
- **Pogođene projektne celine:** ST-009

## Kontekst

ST-009 traži da nastavnik materijalu priloži datoteke. Poslovno pravilo specifikacije: *podržani tipovi i maksimalna veličina priloga su dokumentovani*. US-05 dodaje da nastavnik treba unapred da zna koje datoteke sme da priloži, kako ne bi gubio vreme na odbijene prenose. Bez ograničenja bi bilo moguće priložiti izvršne datoteke ili vrlo velike datoteke, a neuspeli prenos ne sme da ostavi materijal sa nedostajućom datotekom (US-06).

## Odluka

### Podržani tipovi priloga

Dozvoljene su samo datoteke sa sledećim ekstenzijama (lista dozvoljenih; sve ostalo se odbija):

| Ekstenzija | Vrsta sadržaja |
| --- | --- |
| `.pdf` | PDF dokument |
| `.docx` | Word dokument |
| `.pptx` | PowerPoint prezentacija |
| `.xlsx` | Excel tabela |
| `.txt` | običan tekst |
| `.png` | slika |
| `.jpg`, `.jpeg` | slika |
| `.zip` | ZIP arhiva |

### Maksimalna veličina priloga

**20 MB po prilogu** = 20 971 520 bajtova (1 MB = 1024 × 1024 bajtova). Datoteka od tačno 20 MB je dozvoljena.

### Način primene

- Vrednosti se zadaju u `src/Api/appsettings.json`, sekcija `MaterialAttachments` (`AllowedExtensions`, `MaxSizeInBytes`), i učitavaju u `AttachmentUploadPolicy` (Application). Nedostajuća ili neispravna konfiguracija zaustavlja pokretanje API-ja, umesto da se prihvate sve datoteke.
- Tip se određuje po **poslednjoj** ekstenziji izvornog naziva, bez obzira na velika i mala slova (`SKRIPTA.PDF` je dozvoljen, `skripta.pdf.exe` i naziv bez ekstenzije nisu).
- `AttachFileToMaterialHandler` odbija nedozvoljen tip **pre** čuvanja sadržaja.
- Veličina se proverava na **stvarno pročitanim bajtovima**, ne na veličini koju prijavi klijent. Čim pročitani sadržaj pređe maksimum, čitanje se prekida; skladište ne zadržava delimičnu datoteku i zapis priloga se ne pravi. Ako je dužina sadržaja poznata unapred, prevelika datoteka se odbija i pre čuvanja.
- Odbijanje se prijavljuje izuzetkom `AttachmentRejectedException` sa razlogom (`UnsupportedType` / `TooLarge`), nazivom datoteke i porukom za korisnika:
  - `Datoteka 'program.exe' nije priložena: tip datoteke nije dozvoljen. Dozvoljeni tipovi: .docx, .jpeg, .jpg, .pdf, .png, .pptx, .txt, .xlsx, .zip.`
  - `Datoteka 'velika.pdf' nije priložena: veća je od dozvoljenih 20 MB.`
- Prikazana maksimalna veličina zaokružuje se naniže, tako da poruka nikad ne navodi veću granicu od stvarne.

## Posledice

- Odbijena datoteka ne ostavlja ni sadržaj u skladištu ni zapis priloga, u skladu sa US-06.
- Lista tipova i maksimalna veličina menjaju se konfiguracijom, bez izmene koda. **Svaka izmena konfiguracije zahteva i izmenu ovog dokumenta**, jer poslovno pravilo traži da podržani tipovi budu dokumentovani.
- Konstruktor `AttachFileToMaterialHandler` prima `AttachmentUploadPolicy`; budući ulaz za otpremanje (API ili UI) dobija politiku iz composition root-a i poruku prikazuje iz `AttachmentRejectedException.Message`.
- Provera po ekstenziji ne garantuje stvarni sadržaj datoteke (preimenovana datoteka prolazi). To ne zamenjuje kontrolu pristupa (US-12) niti bezbedno rukovanje datotekom pri preuzimanju.
- Naziv datoteke u poruci potiče od korisnika; ulaz koji poruku prikazuje mora da ga prikaže bezbedno (npr. ne kao HTML).

## Povezanost sa specifikacijom

- **Pogođeni zahtevi:** ST-009 (US-05; povezano sa US-04 i US-06)
- **Promena javnih ugovora i zavisnosti:** nova klasa `AttachmentUploadPolicy`, novi `AttachmentRejectedException` i `AttachmentRejectionReason`; konstruktor `AttachFileToMaterialHandler` dobija parametar `AttachmentUploadPolicy`. Portovi skladišta i repozitorijuma nisu menjani.
- **Uticaj na čuvanje i migraciju podataka:** nema migracija; nova sekcija `MaterialAttachments` u `src/Api/appsettings.json`.
- **Testovi koji proveravaju odluku:** `tests/Tests/Application/AttachmentUploadPolicyTests.cs`, `tests/Tests/Application/AttachFileToMaterialHandlerTests.cs`.

## Razmotrene alternative

- **Lista zabranjenih tipova umesto dozvoljenih:** odbijeno, jer bi svaki nepoznat ili novi opasan tip prošao.
- **Veličina iz zaglavlja zahteva ili iz podatka klijenta:** odbijeno kao jedina provera, jer klijent može da pošalje netačnu vrednost.
- **Provera MIME tipa i sadržaja (potpisa) datoteke:** nije deo zahteva; ostaje otvoreno pitanje za tim.
- **Šira lista tipova (npr. `.doc`, `.ppt`, `.mp4`, `.rar`):** tim se dogovorio za užu listu; video sadržaj se može priložiti kao spoljna veza (US-03).

## Otvorena pitanja

- Da li je prazna datoteka (0 B) dozvoljena? Za sada se prihvata.
- Da li proveravati i MIME tip (`ContentType`)? Za sada se ne proverava.
- Provera na klijentu i prikaz pravila pre izbora datoteke čekaju komponentu za otpremanje (US-04).
