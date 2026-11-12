# Tim 5: ST-004 i ST-005

Plan rada za KT1 i KT2. Dijagrami: [arhitektura](arhitektura.png), [DFD](dfd.png).

## 1. Obim

**ST-004 Akademske godine i izvođenja predmeta** (radi administrator)
- Akademska godina: oznaka, datum početka i kraja (početak prethodi kraju).
- Izvođenje povezuje predmet i godinu; par (predmet, godina) je jedinstven.
- Stanja: U pripremi, Aktivno, Završeno. Arhivirano je zasebno svojstvo (ST-091).
- Ništa se ne briše; aktivnosti se vezuju za izvođenje, ne za katalog.

**ST-005 Nastavnici i saradnici na predmetu**
- Administrator dodjeljuje odgovornog nastavnika.
- Odgovorni nastavnik dodaje i uklanja nastavnike i asistente i vidi spisak.
- Aktivno izvođenje mora imati bar jednog odgovornog nastavnika.
- Uklanjanje saradnika čuva autorstvo ranijih zapisa.

## 2. Podjela rada

| # | Dio | Nosilac | Grana |
|---|---|---|---|
| 1 | Domen i baza (entiteti, jedinstvenost para) | | `feature/st-004-domen-baza-tim-5` |
| 2 | ST-004: akademske godine | | `feature/st-004-akademske-godine-tim-5` |
| 3 | ST-004: izvođenja i prelazi stanja | | `feature/st-004-izvodjenja-tim-5` |
| 4 | ST-005: odgovorni nastavnik | | `feature/st-005-odgovorni-nastavnik-tim-5` |
| 5 | ST-005: saradnici | | `feature/st-005-saradnici-tim-5` |
| 6 | Dokumentacija i integracija s drugim timovima | | `feature/st-004-integracija-tim-5` |

Svako: kod u svojoj grani i PR, jedan Tapiz story, bar jedan commit.

## 3. Pravila koja moramo pokriti

| Pravilo | Izvor | Dio |
|---|---|---|
| Početak godine prethodi kraju | ST-004 | 2 |
| Obavezni nazivi nisu prazni | Spec. 2.1 | 2, 3 |
| Par (predmet, godina) jedinstven | ST-004 | 1, 3 |
| Neaktivan predmet ne dozvoljava novo izvođenje | ST-003 | 3 |
| Prelazi samo U pripremi, Aktivno, Završeno | ST-004 | 3 |
| Arhiviranje samo za Završeno | ST-004/091 | 3 |
| Aktivno izvođenje ima bar jednog odgovornog | ST-005 | 4 |
| Odgovorni ne može ukloniti sebe bez zamjene | ST-005 | 4 |
| Saradnik je aktivan nalog uloge Nastavnik ili Asistent | ST-005 | 5 |
| Uklanjanje čuva autorstvo ranijih zapisa | ST-005 | 5 |
| Odbijena operacija ne mijenja podatke | Spec. 2.6 | svi |
| Ovlašćenje se provjerava u servisnom sloju | Spec. 2.1 | svi |

## 4. Zavisnosti od drugih timova

Mi ne implementiramo tuđe dijelove. Koristimo njihove interfejse kad ih isporuče.

| Tema | Šta nam treba | Vlasnik | Status |
|---|---|---|---|
| ST-001/002 | Ko je prijavljen i koja mu je uloga (postojeći `Domain.Users.UserRole`) | tim za ST-001/002 | usaglasiti |
| ST-003 | Postoji li predmet i da li je aktivan (`ICourseReader`) | tim za ST-003 | postoji u repou |
| ST-006 | Upis čita stanje našeg izvođenja | mi izlažemo upit | KT2 |

Dok identitet nije isporučen, u demonstraciji se uloga zadaje ručno i to nije dokaz autentifikacije ni autorizacije.

## 5. Demo scenario za KT1

1. Administrator otvara akademsku godinu 2026/2027.
2. Administrator otvara izvođenje predmeta za tu godinu (U pripremi).
3. Pokušaj duplikata istog para je odbijen, uz razlog.
4. Administrator dodjeljuje odgovornog nastavnika.
5. Odgovorni nastavnik dodaje asistenta; prikaz spiska saradnika.

## 6. Plan po kontrolnim tačkama

**KT1:** projekat se pokreće, početni kod dodijeljenog dijela, lični commit ili PR, dijagram arhitekture i DFD, demo i plan preostalog rada.

**KT2:** sva pravila iz tabele 3, provjera prava pristupa, pozitivni i negativni NUnit testovi, integracija sa ST-001, 002, 003 i 006, ažurirani README i dijagrami, individualna odbrana.

## 7. Test scenariji (NUnit, stil kao `CourseTests`)

- `AcademicYear_RejectsStartAfterEnd`
- `AcademicYear_TrimsLabel`
- `Offering_RejectsDuplicatePair`
- `Offering_RejectsInactiveCourse`
- `Offering_RejectsBackwardStateTransition`
- `Offering_ArchiveRequiresFinished`
- `Staff_CannotRemoveLastResponsibleTeacher`
- `Staff_ResponsibleCannotRemoveSelfWithoutReplacement`
- `Staff_RemovalKeepsAuthorshipOfEarlierRecords`

## 8. Otvorena pitanja

- Nosioci dijelova (popuniti poslije dogovora).
- Usaglašavanje identiteta i uloga s timom za ST-001/002.
- Čuvanje podataka: u memoriji za KT1, baza za KT2 (potvrditi sa asistentom).
