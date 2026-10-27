# Pravila za AI asistente i studente

Ovaj fajl definiše smernice za korišćenje AI alata na projektu STUDIUM. Ne predstavlja zamenu za projektnu specifikaciju, pregled koda ili automatizovane testove.

## Prioriteti

1. Projektna specifikacija STUDIUM, sa zahtevima ST-001–ST-100, određuje poslovna pravila.
2. Izmenjeni zahtev mora biti izričito naveden u issue-u ili PR-u.
3. `CONTRIBUTING.md` i `docs/architecture.md` određuju granice modula i postupak integracije.
4. AI predlozi zahtevaju ljudski pregled i proveru stvarnim komandama.

## Ograničenja AI rada

- Ne predstavljaj demonstracioni `/demo/courses` kao realizovan ST-003.
- Ne implementiraj druge ST zahteve koji nisu u zadatku.
- Ne menjaj ugovore drugih timova bez usaglašavanja.
- Ne čitaj niti otkrivaj tokene, lozinke, privatne ključeve, stvarne podatke studenata i lokalne `.env` datoteke.
- Ne izvršavaj destruktivne komande ili migracije bez izričitog odobrenja.
- Ne tvrdi da je build/test uspešan ako komande nisu izvršene i proverene.
- Ne menjaј glavnu granu bez traženog radnog toka i odgovarajućeg odobrenja.
- Ne dopunjavaj zahteve pretpostavljenim poslovnim pravilima. Nejasnoće izdvoji za diskusiju.

## Očekivani odgovor na razvojni zadatak

- Oznaka ST zahteva i posmatrana poslovna pravila.
- Predlog slojeva/portova koji se menjaju.
- Implementacija ograničenog obima.
- Testovi pozitivnog, negativnog i graničnog ponašanja.
- Rezultat stvarnog `dotnet build` i `dotnet test` ili eksplicitno objašnjenje zašto nisu pokrenuti.
- Kratak sažetak rizika, ograničenja i otvorenih pitanja.

Student je odgovoran za razumevanje svakog predloga i svake promene u repozitorijumu.
