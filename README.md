# STUDIUM: ST-005 Odgovorni nastavnik

Ovaj doprinos pokriva samo deo **„Odgovorni nastavnik"** zahteva ST-005. Dodavanje i uklanjanje saradnika
i pregled spiska rade drugi članovi tima (priča „ST-005: Saradnici na predmetu"), pa nisu deo ovog doprinosa.

## Obim ovog doprinosa
- Administrator dodeljuje odgovornog nastavnika izvođenju.
- Aktivno izvođenje mora imati najmanje jednog odgovornog nastavnika.
- Odgovorni nastavnik ne može ukloniti sebe dok nije određen drugi odgovorni.

## Poslovna pravila (ST-005)
- Aktivno izvođenje mora imati najmanje jednog odgovornog nastavnika.
- Odgovorni nastavnik ne može ukloniti sebe dok nije određen drugi.
- Prekid angažovanja ne briše zapis, nego postavlja `KrajUtc`, pa autorstvo ranijih zapisa ostaje očuvano.

Projektna odluka tima (nije eksplicitno u specifikaciji): ista osoba ne može imati dva aktivna angažovanja na istom izvođenju.

## Pokretanje
Preduslovi: .NET 8 SDK i Git (isto kao u uvodnom delu README-a).

```bash
dotnet restore Solution.sln
dotnet build Solution.sln --configuration Release
dotnet test Solution.sln --configuration Release --filter "FullyQualifiedName~AngazovanjeNastavnikaTests"
```

Ovaj deo nema sopstveni endpoint ni konzolni ulaz, pa se trenutno proverava samo testovima.
Baza nije podešena: šablon za sada koristi memorijski adapter.

## Dijagrami
- [Dijagram arhitekture](<docs/dijagram arhitekture ST-005 Odgovorni nastavnik.jpeg>)
- [Dijagram toka podataka (DFD)](<docs/dijagram toka podataka(DFD) ST 005 Odgovorni nastavnik.jpeg>)

Dijagrami prikazuju ciljno stanje celog ST-005 (uključujući saradnike), a ne samo ono što je implementirano u KT1.

## Zavisnosti od drugih delova
- ST-001: korisnički nalozi (aktivan nalog odgovarajuće uloge)
- ST-002: prijava i provera ovlašćenja (ko je prijavljen i koje je uloge)
- ST-004: izvođenja predmeta (stanje izvođenja; priča „ST-004: Domen i baza" sadrži entitet `TeachingAssignment`)
- ST-089: evidencija promena (planirano)

## Stanje implementacije (KT1)
Urađeno (samo domenski sloj, `src/Domain/Courses/ST005Magdalena.cs`):
- Entitet `AngazovanjeNastavnika` sa validacijom pri kreiranju, bez javnih settera, i prekid angažovanja umesto brisanja.
- Enum `UlogaNaPredmetu` (OdgovorniNastavnik, Nastavnik, Asistent).
- Pravila u `OdgovorniNastavnikPravila`: provera dodele i provera prekida (bar jedan odgovorni na aktivnom izvođenju).
- NUnit testovi domena: `AngazovanjeNastavnikaTests`.
- Dijagram arhitekture i DFD.

Nije urađeno (planirano do KT2):
- Slučaj upotrebe „dodela odgovornog nastavnika" u `Application` sa portovima ka ST-001/ST-004.
- Perzistencija: migracija i unique indeks. Treba uskladiti sa članom koji radi `TeachingAssignment` (ST-004: Domen i baza), da se entitet ne duplira.
- API/konzolni ulaz.
- Provera ovlašćenja (ST-002): samo administrator dodeljuje odgovornog.
- Dodavanje i uklanjanje saradnika i spisak: odgovornost priče „ST-005: Saradnici na predmetu".

## Demo (KT1)
Pokrenuti testove iz odeljka „Pokretanje". Testovi pokazuju:
1. Pokušaj prekida jedinog odgovornog nastavnika na aktivnom izvođenju se odbija.
2. Prekid je dozvoljen kada postoji drugi aktivni odgovorni.
3. Isti korisnik ne može dvaput biti aktivan na istom izvođenju.
