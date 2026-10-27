# Početna arhitektura STUDIUM-a

Ovo je **nastavni kostur**, a ne konačni model sistema. Struktura namerno sadrži jedan demonstracioni tok. Student razvija modele i ugovore prema dodeljenim zahtevima, uz očuvanje smerova zavisnosti.

```text
Studium.Api ───────────┐
                       ├──> Studium.Application ───> Studium.Domain
Studium.ConsoleUi ─────┘              ^
                                      │ implementira portove definisane ovde
                           Studium.Infrastructure
```

Tehnički `Infrastructure` referencira `Application`, a `Application` definiše interfejs `ICourseReader`. Tok izvršavanja je `Api/ConsoleUi → ListCoursesHandler → ICourseReader → InMemoryCourseReader`. Domenski tip `Course` ne zavisi od tehnologije skladištenja.

## Koncepti iz specifikacije

- **Predmet** (kataloški zapis) i **izvođenje predmeta** (predmet u konkretnoj akademskoj godini) nisu isti entitet.
- Članstvo, angažovanje nastavnog osoblja, zadaci, predaje i rezultati vezuju se za odgovarajuće izvođenje.
- Autorizacija mora da obuhvati identitet, ulogu, članstvo i dozvolu nad konkretnim zapisom.
- Nedostajući rezultat i rezultat sa nula bodova nisu isto stanje.
- Neuspešna operacija ne sme parcijalno promeniti prethodno važeće stanje.
- Sistemski prikazi i obaveštenja ne smeju otkriti podatke koji korisniku nisu dostupni.

## Šta NIJE pripremljeno

Autentifikacija, autorizacija, baza podataka, migracije, frontend, učitavanje priloga, raspored, predaje, ocenjivanje, kvizovi i ostali poslovni tokovi nisu implementirani. To su delovi studentskog rada. `/health` potvrđuje samo da API proces odgovara, ne stanje buduće baze.

## Primer širenja strukture

```text
src/Studium.Domain/
  Courses/
  Offerings/               # model izvođenja predmeta — studenti dodaju
  Submissions/             # model predaja — studenti dodaju

src/Studium.Application/
  Courses/
  Offerings/               # slučajevi upotrebe i portovi — studenti dodaju

src/Studium.Infrastructure/
  Courses/                 # demonstracioni memorijski adapter
  Persistence/             # trajno skladištenje tek nakon odluke o dizajnu
```

Pre uvođenja baze i dodatnih modula obavezno zapisati odluku u ADR: vlasništvo nad podacima, identitet entiteta, veze, transakcione granice i način testiranja. ER i UML modeli u projektnoj specifikaciji služe kao **smernice**, ne kao automatski zadat fizički model baze.
