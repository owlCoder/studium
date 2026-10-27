# Prvi doprinos STUDIUM projektu

Ovaj vodič prikazuje razvojni tok kroz **jedan mali zahtev**. Ne podrazumeva da je dodeljeni zahtev implementiran u početnom repozitorijumu.

## 1. Priprema

Potrebni su .NET 8 SDK i Git. Nakon preuzimanja:

```bash
dotnet restore Solution.sln
dotnet build Solution.sln --configuration Release
dotnet test Solution.sln --configuration Release
dotnet run --project src/ConsoleUi
```

Za HTTP demonstraciju pokrenuti `dotnet run --project src/Api` i proveriti `GET /health` i `GET /demo/courses`. Ovi endpointi služe za upoznavanje sa infrastrukturom i **ne predstavljaju završene STUDIUM funkcionalnosti**.

## 2. Pročitaj zahtev

Pronaći dodeljenu oznaku u [indeksu](requirements/ST-INDEX.md), zatim pročitati **ceo opis u projektnoj specifikaciji**, uključujući učesnike, podatke, povezana pravila i primer.

Pre kodiranja napisati proverljive kriterijume: uspešan tok, odbijeni tok i granični slučajevi. Na primer, za ST-003 razmotriti jedinstvenost oznake i zabranu prekida postojećih veza pri promeni naziva — precizan obim određuje specifikacija.

## 3. Granice slojeva

- Domenske invarijante i modeli: `Domain`.
- Slučajevi upotrebe i portovi: `Application`.
- Baza/datoteke/spoljni adapteri: `Infrastructure`.
- HTTP i konzolni ulaz, sastavljanje zavisnosti: `Api` i `ConsoleUi`.
- NUnit testovi: `Tests`.

Ne dodavati novu klasu samo radi broja slojeva; svaki ugovor treba da ima jasnu potrebu.

## 4. Grana i Pull Request

```bash
git switch main
git pull --ff-only
git switch -c feature/st-xxx-kratak-opis
# ... promena koda i testova ...
dotnet build Solution.sln --configuration Release
dotnet test Solution.sln --configuration Release
git status
git diff
git add .
git commit -m "feat: implement ST-xxx ..."
git push -u origin feature/st-xxx-kratak-opis
```

Otvoriti PR prema `main` i popuniti šablon. U PR-u obrazložiti šta je implementirano, kako se proverava, koji javni ugovori su promenjeni i šta je ostalo van obima. Odobrava se tek kada pregled i CI uspeju.

## 5. Dokaz, ne samo prolazan build

Pozitivan ishod testova ne dokazuje sva poslovna pravila. Kod zahtevnih operacija proveriti i autorizaciju nad konkretnim zapisom, konkurentne zahteve, idempotentnost i stanje nakon odbijene operacije — gde to specifikacija zahteva.

Za značajnu odluku kopirati [ADR šablon](templates/adr.md) i dokumentovati alternativna rešenja.
