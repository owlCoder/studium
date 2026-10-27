# Praćenje zahteva STUDIUM-a

**Izvor istine** je projektna specifikacija predmeta sa zahtevima `ST-001` do `ST-100`. Za brzu navigaciju koristi [indeks naziva svih 100 zahteva](ST-INDEX.md). Ovaj repozitorijum ne zamenjuje tekst zahteva. Svaka izmena u kodu treba da navede relevantnu oznaku ST-xxx, kriterijume prihvatanja, testove i pogođene komponente.

## Predlog evidencije

| Polje | Primer |
| --- | --- |
| Zahtev | `ST-003` |
| Poslovno pravilo | Oznaka predmeta je jedinstvena |
| Implementacija | navesti putanje do stvarnih klasa nakon implementacije |
| Test | navesti NUnit test za duplikat |
| Status | nije započeto / u toku / provereno |
| Povezani zahtevi | prema specifikaciji |

## Predložene funkcionalne grupe (za organizaciju timova)

- Korisnici, uloge i izvođenja predmeta
- Materijali, obaveštenja, lekcije i pristup sadržaju
- Zadaci, predaje, pregledi i rezultati
- Nastavne grupe, studentski timovi i projekti
- Nastavni termini, prisustvo, konsultacije i raspored
- Kvizovi, pitanja, samoprovere i ankete
- Datoteke, pretraga, istorija i izvoz
- Administrativni procesi, arhiviranje i organizacija ustanove

Grupe su samo predlog za raspodelu rada. **Ne podrazumevaju preciznu podelu zahteva** niti promenu značenja bilo kog ST-xxx.

## Kontrola pre zatvaranja zahteva

1. Da li implementacija sprovodi sve navedene poslovne uslove?
2. Da li odbijena operacija ostavlja podatke nepromenjenim?
3. Da li student može pristupiti isključivo dozvoljenim zapisima?
4. Da li je proverena jedinstvenost i konkurentnost tamo gde je potrebna?
5. Da li su dodati testovi za uspeh, neuspeh i granične slučajeve?
6. Da li promena zavisi od odgovarajućeg izvođenja predmeta i akademske godine?
7. Da li PR pokazuje vezu između zahteva, implementacije i provere?

Početni `/demo/courses` je isključivo **ilustracija arhitekture**, ne označavati `ST-003` kao implementiran.
