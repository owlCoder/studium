# PYXIS

PYXIS je informacioni sistem za upravljanje Data centrom. Platforma povezuje
model fizičke infrastrukture, IT resurse, korisničke organizacije, kapacitete,
rezervacije, monitoring, incidente, održavanje i poslovne procese data centra.

Ovaj repozitorijum je zajedničko mesto za razvoj projekta u okviru predmeta
**Elementi razvoja softvera** na Fakultetu tehničkih nauka – Primenjeno
softversko inženjerstvo.

## Rad na projektu

Projektne celine, zahtevi i kriterijumi dostavljaju se studentima kroz zasebnu
projektnu specifikaciju u PDF formatu i kroz zadatke u Tapiz Boards-u.

- Za svaki zadatak napravite feature granu iz `main`.
- Promene se predaju Pull Request-om; direktan push na `main` nije dozvoljen.
- Pre merge-a potreban je review i uspešne provere.
- Izmene ugovora između celina uskladite sa pogođenim timovima.
- Ne commitujte lozinke, tokene, sertifikate ni druge tajne.

Detaljna pravila rada nalaze se u [CONTRIBUTING.md](CONTRIBUTING.md). Šabloni za
Pull Request i arhitektonske odluke nalaze se u ovom repozitorijumu.

## Predložena struktura

```text
src/            aplikacioni kod
tests/          automatizovani testovi
docs/           ADR-ovi i dokumentacija po projektnim celinama
.github/        GitHub šabloni i CI konfiguracija
```
