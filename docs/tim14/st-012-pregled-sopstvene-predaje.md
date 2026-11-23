# ST-012 — Pregled sopstvene predaje

Kao student želim da za izabrani individualni zadatak pogledam svoju važeću predaju, kako bih proverio šta sam predao i kada.

## Kriterijumi prihvatanja

| Kriterijum | Status | Implementacija i automatska provera |
| --- | --- | --- |
| 1. Student vidi svoju važeću predaju za izabrani zadatak. | Ispunjen | `GetOwnSubmissionHandler` traži predaju po zadatku i identitetu iz prijave. `Get_ReturnsOnlyOwnSubmissionDetails` i `FindCurrentAsync_UsesBothAssignmentAndStudent` proveravaju izbor predaje. |
| 2. Prikazuju se tekst, prilozi i vreme. | Ispunjen | `OwnSubmissionController` vraća tekst, vreme i metapodatke priloga. `ViewOwnSubmissionScenario` ih prikazuje. Proveravaju ih `Get_ReturnsOnlyOwnSubmissionDetails` i `Console_PrintsOwnSubmissionOrNotFound(true)`. |
| 3. Posle zamene se vidi nova važeća predaja. | Delimično ispunjen | Handler ponovo čita važeću predaju pri svakom pozivu. `HandleAsync_ReadsCurrentSubmissionOnEveryRequest` simulira promenu odgovora repozitorijuma. Nedostaju stvarna zamena i integracioni test te promene. |
| 4. Kada nema predaje, student dobija jasnu poruku. | Ispunjen | API vraća `404`, a konzola prikazuje poruku. Proveravaju ih `Get_ReturnsNotFoundWhenOwnSubmissionDoesNotExist` i `Console_PrintsOwnSubmissionOrNotFound(false)`. |
| 5. Student ne može da pročita tuđu predaju. | Ispunjen u okviru identiteta iz prijave | Handler ograničava upit i proverava vlasnika rezultata. `Get_QueryCannotSelectAnotherStudentsSubmission` proverava da parametar `studentId` ne može promeniti identitet. `Get_RequiresAuthentication` i `Production_DisablesDemoAuthenticationAndDemoSeed` proveravaju granicu demo prijave. |

## Od prijave do repozitorijuma

- `HttpCurrentStudent` implementira `ICurrentUser` i čita `NameIdentifier` autentifikovanog korisnika. Nedostajući ili nevažeći identitet zaustavlja čitanje.
- `GetOwnSubmissionHandler` prosleđuje ID zadatka i prijavljenog studenta u `ISubmissionRepository.FindCurrentAsync`. Dodatno proverava da vraćeni zapis pripada tom studentu i zadatku.
- `InMemorySubmissionRepository` je jedna instanca po procesu API-ja. Za par zadatak–student čuva najviše jednu predaju, koja je ujedno važeća. `FindCurrentAsync` vraća tu predaju ili `null`. Ponovni `AddAsync` za isti par odbija upis i čuva prethodni zapis.
- `OwnSubmissionController` mapira rezultat na HTTP odgovor. `ViewOwnSubmissionScenario` poziva tu rutu i prikazuje rezultat. `SubmissionDemoData` priprema sintetičku predaju za lokalnu demonstraciju.

Kada tim uvede zamenu, implementacija čuvanja treba da označi novu važeću predaju tako da je `FindCurrentAsync` vrati pri sledećem čitanju. Tada treba dodati integracioni test koji izvrši zamenu pa ponovi pregled istim studentom.

## API

`GET /api/assignments/{assignmentId}/submissions/me`

`assignmentId` je GUID zadatka iz rute. Identitet dolazi iz prijave.

| Status | Značenje |
| --- | --- |
| `200 OK` | `text`, `submittedAt` (datum i vreme sa vremenskim pomakom) i `attachments` sa poljima `fileName`, `fileType` i `size` u bajtovima. |
| `400 Bad Request` | ID zadatka je neispravan ili je prazan GUID. |
| `401 Unauthorized` | Nema važećeg autentifikovanog identiteta. |
| `404 Not Found` | Za prijavljenog studenta nema predaje za taj zadatak. |

Demo prijava koristi sintetički identitet iz zaglavlja `X-Demo-UserId` i aktivna je samo u okruženjima `Development` i `KT1`. Zaglavlje služi lokalnom testiranju i ne dokazuje identitet u produkciji. Demo zapis se učitava samo uz `--DemoSubmissions=true` u dozvoljenom okruženju. Memorijski podaci se gube gašenjem API-ja.

## Lokalna demonstracija

Iz korena repozitorijuma, u prvom PowerShell terminalu:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Api --configuration Release -- --urls http://127.0.0.1:5098 --DemoSubmissions=true
```

U drugom terminalu:

```powershell
dotnet run --project src/ConsoleUi --configuration Release -- st-012
```

1. Unesi `12121212-0000-0000-0000-000000000001`. Očekuj tekst „Primer predaje individualnog rada.“, prilog `primer.pdf` (`application/pdf`, 2048 B) i vreme `23.11.2026 12:00:00 +01:00`.
2. Ponovo pokreni konzolu i unesi `12121212-0000-0000-0000-000000000003`. Očekuj „Nemaš predaju za ovaj zadatak.“
3. Ponovo pokreni konzolu i unesi `nije-guid`. Očekuj „Identifikator zadatka nije ispravan.“ Prazan unos zatvara scenario.

## Testovi

```powershell
dotnet build Solution.sln --configuration Release
dotnet test Solution.sln --configuration Release
dotnet test Solution.sln --configuration Release --filter 'FullyQualifiedName~OwnSubmissionApiTests|FullyQualifiedName~GetOwnSubmissionHandlerTests|FullyQualifiedName~InMemorySubmissionRepositoryTests'
```

API i konzolni testovi za uspešan pregled i odsustvo predaje koriste stvarni memorijski repozitorijum u testnom hostu. Za poruke pri nedostupnoj vezi i neuspešnim HTTP odgovorima testovi simuliraju samo HTTP transport.

## Integracija sa drugim izmenama

- **PR #98.** U `src/Api/Program.cs` zadržati registracije i za zadatke i za predaje. Jedan poziv `AddHttpContextAccessor` dovoljan je za oba toka.
- **PR #99.** U `src/ConsoleUi/Program.cs` zadržati `st-011`, `st-012`, `st-031-032` i podrazumevani katalog. U `Tests.csproj` ostaviti jednu referencu na `ConsoleUi`. Sačuvati LF pravilo za konzolni `Program.cs`.
- **PR #106.** Provere članstva i praznog unosa pripadaju slanju rada. Kada se tok slanja poveže sa API-jem, upis i pregled treba da koriste isti `ISubmissionRepository`. Kriterijum zamene ostaje otvoren.
- **PR #109.** Dva backend commita su osnova ove grane. Pri integraciji sačuvati postojeći handler, port i memorijski adapter.
