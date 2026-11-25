# ADR-0001: Podobnost studenata za evidenciju prisustva

- **Status:** Predložen
- **Datum:** 2026-11-23
- **Vlasnici:** tim ST-031/ST-032
- **Pogođene projektne celine:** evidencija prisustva (ST-032); upis studenata (ST-006); nastavne grupe (ST-019)

## Kontekst

ST-032 kaže: „Podobnost se utvrđuje prema članstvu i grupi u vreme termina.” Spisak za prisustvo mora da sadrži tačno one studente koji su u vreme termina bili u ciljnoj grupi. Za termin namenjen celom izvođenju to su studenti celog izvođenja.

Podatke o članstvu vode drugi timovi: upis na izvođenje je ST-006, a pripadnost grupi ST-019. Naš modul ih samo čita, preko porta `IEligibilityProvider` (`src/Application/Attendance`).

Posle KT1 memorijska zamena porta je imala dva nedostatka:

- ignorisala je vreme termina;
- za termin celog izvođenja je vraćala samo studente bez grupe.

## Odluka

1. **Ugovor porta se ne menja.** `IEligibilityProvider.ListEligibleStudentIdsAsync(offeringId, groupId, at)` ostaje isti, pa timovi ST-006 i ST-019 ne moraju ništa da menjaju u postojećem dogovoru.
2. **Pravilo podobnosti je u domenu** (`Domain.Attendance.EligibilityRule`), a ne u memorijskoj zameni. Prava implementacija drugog tima može ga koristiti direktno ili mora da prođe iste testove.
3. **Članstvo ima period važenja** (`MembershipPeriod`):
   - početak je obavezan, a kraj je opcion;
   - kraj mora biti posle početka;
   - početak je uključen, a kraj isključen: `[od, do)`.
4. **Trenutak provere je početak termina** (`TeachingSession.StartsAt`), kao i u postojećem `RecordAttendanceHandler`.
5. **Ciljna grupa termina:**
   - termin za grupu obuhvata članove te grupe;
   - termin za celo izvođenje (`groupId = null`) obuhvata članove svih grupa izvođenja i članove bez grupe;
   - svaki student se pojavljuje najviše jednom.

## Posledice

- Promena grupe tokom semestra (primer V1 → V2 iz ST-029) automatski se odražava na spiskove: pre prelaska student je na spisku V1, a posle prelaska na spisku V2.
- Postojeći pozivi `InMemoryEligibilityProvider.Add(offering, group, student)` i dalje rade: takvo članstvo važi uvek.
- **Odgovor asistenta (24.11.2026):** student prebačen iz jedne grupe u drugu odmah se računa u novoj grupi. To potvrđuje pravilo `[od, do)`: staro članstvo prestaje, a novo počinje u istom trenutku. Provera ostaje u odnosu na početak termina.
- **Odgovor asistenta (24.11.2026):** termin bez ciljne grupe obuhvata sve upisane studente izvođenja, iz svih grupa.

## Povezanost sa specifikacijom

- **Pogođeni zahtevi:** ST-032 (povezano: ST-006, ST-019, ST-029, ST-031)
- **Promena javnih ugovora i zavisnosti:** nema; port `IEligibilityProvider` ostaje isti. Od timova ST-006 i ST-019 tražimo potvrdu da mogu da obezbede članstvo sa periodom važenja (od, do) po izvođenju i grupi.
- **Uticaj na čuvanje i migraciju podataka:** nema; podaci se samo čitaju.
- **Testovi koji proveravaju odluku:**
  - `tests/Tests/Domain/Attendance/MembershipPeriodTests.cs`
  - `tests/Tests/Domain/Attendance/EligibilityRule*Tests.cs`
  - `tests/Tests/Application/Attendance/Eligibility*Tests.cs`

## Razmotrene alternative

- **Postojeći port `Application.Access.IMembershipReader` (tim ST-018).** Odbijeno: vraća samo da/ne za aktivno članstvo na izvođenju, bez grupe i bez perioda važenja, pa ne može da odgovori ko je bio u kojoj grupi u vreme termina.
- **Novi port koji vraća celo članstvo sa periodima, a pravilo računa naš modul.** Ovo bi menjalo ugovor sa drugim timovima pre dogovora, što pravila projekta ne dozvoljavaju.
- **Pravilo samo u memorijskoj zameni.** Odbijeno: pravilo bi postojalo samo u testovima i ne bi bilo deo domena.
- **Provera na kraju termina umesto na početku.** Odbijeno, jer postojeći kod već koristi početak termina.
