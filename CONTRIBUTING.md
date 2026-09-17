# Doprinos projektu PYXIS

## Tok rada

1. Preuzmite zadatak dodeljen vašem timu u Tapiz Boards-u.
2. Ažurirajte lokalni `main` i napravite granu formata
   `feature/<celina>-<kratak-opis>` (na primer `feature/R1-01-facilities`).
3. Radite u manjim, smislenim commit-ovima.
4. Pokrenite relevantne build i test komande lokalno.
5. Otvorite Pull Request prema `main`, popunite PR šablon i povežite zadatak.
6. Odgovorite na review komentare; merge radi ovlašćeni član nakon odobrenja i
   uspešnog CI-ja.

## Pravila integracije

- Nemojte direktno pushovati na `main`.
- PR treba da bude fokusiran na jednu celinu ili jasno odvojenu epiku.
- Svaka celina ima jasno vlasništvo nad poslovnim podacima i ugovorima.
- Promena javnog ugovora, zajedničkog modela ili cross-team integracije zahteva
  review timova koje promena pogađa.
- Multi-tenant podaci i poslovna pravila moraju čuvati tenant kontekst u svim
  relevantnim operacijama.
- CI mora proći pre merge-a.

## Arhitektonske odluke

Za značajnu odluku dodajte kratak ADR u `docs/adr/` koristeći
[šablon](docs/templates/adr.md). Dokumentujte kontekst, odluku, posledice i
uticaj na druge celine.

## Tajne i testni podaci

Nikada ne commitujte stvarne lozinke, API tokene, privatne ključeve ili
produkcione sertifikate. Koristite `.env.example`, testne vrednosti i simulatore.
Ako je tajna slučajno commitovana, odmah obavestite nastavni tim; nemojte je
samo obrisati u sledećem commit-u.
