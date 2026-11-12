# ST-009 + ST-010 — poslovni scenario

- **ST-009 Nastavni materijali:** nastavno osoblje uređuje materijal (naziv, opis, prilozi, spoljne veze) i objavljuje ga ili povlači. Student pregleda opis i preuzima dostupne priloge.
- **ST-010 Organizacija nastavnih materijala:** nastavnik formira tematske celine i fascikle, menja redosled i premešta materijale. Student koristi istu strukturu, sa sadržajem koji mu je dostupan.
- **Dijagrami:** [arhitektura](dijagram-arhitekture.png) · [DFD nivo 1](dfd.png)

## Poslovni scenario

Izvođenje predmeta ERS 2026/27. Svi akteri su sintetički.

| # | Korak | Zahtev | Stanje |
| --- | --- | --- | --- |
| 1 | Nastavnik kreira materijal „Vežbe 1” sa opisom. Materijal se čuva kao **nacrt**; studenti ga ne vide. Prazan naziv se odbija. | ST-009 | urađeno |
| 2 | Nastavnik menja naziv i opis materijala; beleži se vreme poslednje izmene. | ST-009 | urađeno |
| 3 | Nastavnik dodaje spoljnu vezu (puna adresa) ka video snimku. | ST-009 | urađeno |
| 4 | Nastavnik prilaže dve datoteke (PDF i ZIP). Datoteka nedozvoljenog tipa ili prevelika datoteka se odbija sa jasnom porukom. | ST-009 | urađeno |
| 5 | Slanje druge datoteke ne uspe. Sistem **ne prijavljuje** da su obe sačuvane i ne ostavlja zapis priloga bez datoteke. | ST-009 | urađeno |
| 6 | Korisnik koji nije osoblje tog izvođenja pokuša da kreira materijal i biva odbijen. | ST-009, ST-005 | urađeno za kreiranje; čeka integraciju ST-002 i ST-005 |
| 7 | Nastavnik pravi fasciklu „Vežbe”, u njoj fasciklu „Vežba 1” i u nju premešta materijal. Identifikator materijala se ne menja. | ST-010 | planirano |
| 8 | Nastavnik pokuša da premesti „Vežbe” u „Vežba 1”. Sistem odbija, jer fascikla ne može postati sopstveni potomak. | ST-010 | planirano |
| 9 | Nastavnik objavljuje materijal. Student tog izvođenja ga vidi u istoj strukturi fascikli i preuzima priloge. | ST-009, ST-010 | planirano |
| 10 | Student drugog izvođenja otvori direktnu adresu priloga i biva odbijen. | ST-009, ST-006 | planirano |
| 11 | Nastavnik povlači materijal; studenti ga više ne vide, ni direktnom adresom. | ST-009 | planirano |

Koraci 1–6 mogu se pokazati kroz `dotnet run --project src/ConsoleUi`, HTTP endpointe u `src/Api` i testove (`dotnet test`).

## Poslovna pravila

**ST-009**
- Materijal pripada konkretnom **izvođenju** predmeta, ne kataloškom predmetu.
- Naziv je obavezan; opis nije. Novi materijal je nacrt.
- Nacrt i povučen materijal nisu dostupni studentima, ni preko identifikatora.
- Za prilog se čuvaju izvorni naziv, tip i veličina. Spoljne veze su pune adrese.
- Prilog nasleđuje pravo pristupa materijala.
- Neispravan prenos ne ostavlja objavljen materijal sa nedostajućom datotekom.
- Podržani tipovi i maksimalna veličina priloga su dokumentovani (ADR-0002, `appsettings.json`).

**ST-010**
- Fascikla ima naziv, opcionu nadređenu fasciklu, redosled i pripadajuće materijale.
- Redosled se određuje unutar istog nivoa strukture.
- Fascikla ne može postati sopstveni potomak.
- Uklanjanje neprazne fascikle zahteva prethodno premeštanje sadržaja.
- Premeštanje ne menja identifikator materijala.

**Zajedničko**
- Uređivanje sme samo nastavno osoblje tog izvođenja; pregled samo član tog izvođenja.
- Pravo se proverava nad konkretnim zapisom; skrivanje u UI nije bezbednosna kontrola.
- Odbijena operacija ne menja prethodno važeće stanje.

## Preostali rad

- ST-009: objava i povlačenje materijala.
- ST-009: pregled i preuzimanje za studente, samo objavljeno i samo za članove izvođenja.
- ST-009: provera osoblja i za izmenu, priloge i veze, ne samo za kreiranje.
- ST-010: model fascikle/celine, kreiranje, redosled, premeštanje, uklanjanje i provera ciklusa.
- ST-010: prikaz iste strukture studentu, sa samo dostupnim sadržajem.
- Trajno skladište (baza) po zajedničkoj odluci projekta.

## Zavisnosti od drugih delova

| Deo | Šta je potrebno | Za šta |
| --- | --- | --- |
| ST-002 Prijava | identitet prijavljenog korisnika | svaka operacija nad materijalima |
| ST-005 Nastavnici i saradnici | da li je korisnik osoblje izvođenja | uređivanje materijala i fascikli |
| ST-006 Upis studenata | da li je student član izvođenja | pregled i preuzimanje |
| ST-004 / ST-005 Izvođenje | izvođenje predmeta i njegov identifikator | materijal i fascikla pripadaju izvođenju |
