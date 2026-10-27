# Smernice za početni model podataka STUDIUM-a

Ovaj dokument je **konceptualni nagoveštaj** za studentsko projektovanje, a ne unapred data SQL šema. Tačne tabele, ključeve, migracije i ograničenja određuju studenti na osnovu specifikacije ST-001–ST-100.

## Polazni pojmovi i veze

| Pojam | Uloga | Tipična veza |
| --- | --- | --- |
| `Course` / Предмет | Kataloški opis predmeta | Jedan predmet ima više izvođenja |
| `AcademicYear` / Академска година | Okvir jednog nastavnog ciklusa | Jedna godina obuhvata više izvođenja |
| `CourseOffering` / Извођење | Predmet u konkretnoj godini | Veza prema predmetu i akademskoj godini |
| `User` / Корисник | Nalog sa sistemskom ulogom | Članstva i angažovanja su zasebni zapisi |
| `Enrollment` / Чланство | Veza studenta sa izvođenjem | Veza studenta i konkretnog izvođenja |
| `TeachingAssignment` / Ангажовање | Ovlašćenje osoblja na izvođenju | Uloga i opseg prava po izvođenju |
| `Assignment` / Задатак | Nastavna obaveza na izvođenju | Više predaja za zadatak |
| `Submission` / Предаја | Predati rad, autor i vreme | Pripada zadatku; istorija verzija mora biti očuvana |
| `Review` / Преглед | Povratna informacija o određenoj predaji | Ne znači automatski rezultat |
| `Grade` / Резултат | Evidencija bodova na aktivnosti | Razlikovati odsutan rezultat od nule |

Obavezno razlikovati kataloški predmet od njegovog izvođenja. Članstvo u izvođenju za jednu akademsku godinu ne daje pristup svim izvodjenjima istog predmeta.

## Kandidati za ograničenja

- Stabilni primarni ključevi entiteta; naziv nije primarni ključ.
- Jedinstvena oznaka kataloškog predmeta.
- Jedinstven par `(course_id, academic_year_id)` za izvođenje predmeta.
- Jedno važeće članstvo studenta na izvođenju, uz očuvanje istorije promene statusa.
- Referencijalni integritet predaje, zadatka, autora i relevantnog izvođenja.
- Pravilo ocene: `0 <= bodovi <= maksimum`; vrednost `NULL` ne zamenjuje nula bodova.
- Vreme početka intervala mora prethoditi završetku. Na nivou konkurentnih rezervacija pravila moraju sprečiti dvostruko dodeljivanje resursa.

Ovo su **kandidati** za model i kontrolne tačke, ne tvrdnja da sve treba realizovati jednim prostim SQL ograničenjem.

## Granice transakcija i dostupnosti

- Odvojiti identitet, aktivno članstvo i prava pristupa konkretnom zapisu.
- Nije dozvoljeno pretraživanjem ili eksportom otkriti studentu naцrte i podatke drugih grupa.
- Odbijena operacija ne menja prethodno važeće stanje.
- Pri objavljivanju ocena razlikovati radni i javno dostupni rezultat.
- Za prikaze kalendara i izveštaja definisati izvorne evidencije; ne uvoditi samostalne kopije koje mogu zastareti.
- Pri konkurentnim prijavama i rezervacijama odrediti transakcionu granicu i objasniti kako se sprečava prekapacitet.

## Pre prve migracije

1. Nacrtati ER model izabrane oblasti i objasniti kardinalitete.
2. Označiti primarne i strane ključeve i pravila jedinstvenosti.
3. Prepoznati osetljive podatke i uslove pristupa.
4. Napisati scenarije testiranja za brisanje, deaktivaciju, promenu godine, duplikate i istovremene operacije.
5. Obrazložiti izbor skladišta i migracija u ADR-u.

Priložene UML/ER ilustracije iz specifikacije treba tumačiti kao smernice, a ne kao obavezan fizički model.
