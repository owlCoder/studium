using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Infrastructure.Demo;

namespace ConsoleUi.Scenarios;

public static class CreateAssignmentScenario
{
    private const string DeadlineFormat = "dd.MM.yyyy HH:mm zzz";

    public static async Task RunAsync(HttpClient client, TextReader input, TextWriter output)
    {
        output.WriteLine("STUDIUM | Kreiranje individualnog zadatka (ST-011, demo nastavnik)");
        output.WriteLine($"Izvođenje ERS {DemoData.ErsId}");
        output.WriteLine($"Izvođenje OIB {DemoData.OibId}");
        output.Write("Unesi ID izvođenja ili ostavi prazno za izlaz: ");
        var offering = input.ReadLine();
        if (string.IsNullOrWhiteSpace(offering))
            return;

        if (!Guid.TryParse(offering, out var offeringId) || offeringId == Guid.Empty)
        {
            output.WriteLine("Identifikator izvođenja nije ispravan.");
            return;
        }

        string? title = null;
        string? description = null;
        string? instructions = null;
        DateTimeOffset? deadline = null;

        while (true)
        {
            output.WriteLine();
            output.WriteLine($"1. Naziv: {title}");
            output.WriteLine($"2. Opis: {description}");
            output.WriteLine($"3. Uputstvo: {instructions}");
            output.WriteLine($"4. Rok: {deadline?.ToString(DeadlineFormat, CultureInfo.InvariantCulture)}");
            output.WriteLine("5. Potvrdi i sačuvaj nacrt");
            output.WriteLine("0. Odustani");
            output.Write("Izaberi opciju: ");

            switch (input.ReadLine()?.Trim())
            {
                case "1":
                    output.Write("Novi naziv: ");
                    title = input.ReadLine();
                    break;
                case "2":
                    output.Write("Novi opis: ");
                    description = input.ReadLine();
                    break;
                case "3":
                    output.Write("Novo uputstvo: ");
                    instructions = input.ReadLine();
                    break;
                case "4":
                    output.Write("Rok (npr. 15.01.2027 18:30 +01:00, prazno za brisanje): ");
                    var value = input.ReadLine();
                    if (string.IsNullOrWhiteSpace(value))
                        deadline = null;
                    else if (DateTimeOffset.TryParseExact(value.Trim(), DeadlineFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDeadline))
                        deadline = parsedDeadline;
                    else
                        output.WriteLine("Rok nije ispravan. Prethodna vrednost je zadržana.");
                    break;
                case "5":
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        output.WriteLine("Naziv zadatka je obavezan.");
                        break;
                    }

                    using (var request = new HttpRequestMessage(HttpMethod.Post, $"api/offerings/{offeringId}/assignments"))
                    {
                        request.Headers.Add("X-Demo-UserId", DemoData.ErsTeacherId.ToString());
                        request.Content = JsonContent.Create(new { title, description, instructions, deadline });
                        await SaveDraftAsync(client, request, output);
                    }
                    return;
                case "0":
                case null:
                    output.WriteLine("Unos je otkazan.");
                    return;
                default:
                    output.WriteLine("Izaberi jednu od ponuđenih opcija.");
                    break;
            }
        }
    }

    private static async Task SaveDraftAsync(HttpClient client, HttpRequestMessage request, TextWriter output)
    {
        try
        {
            using var response = await client.SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    var result = await response.Content.ReadFromJsonAsync<CreateAssignmentResponse>();
                    output.WriteLine(result == null || result.AssignmentId == Guid.Empty
                        ? "API je prijavio da je nacrt sačuvan, ali odgovor nema ID zadatka."
                        : $"Nacrt je sačuvan. ID zadatka: {result.AssignmentId}");
                    break;
                case HttpStatusCode.BadRequest:
                    output.WriteLine("API je odbio podatke zadatka.");
                    break;
                case HttpStatusCode.Unauthorized:
                    output.WriteLine("Demo prijava nije prihvaćena. Proveri okruženje API-ja.");
                    break;
                case HttpStatusCode.Forbidden:
                    output.WriteLine("Nemaš pravo da kreiraš zadatak za ovo izvođenje.");
                    break;
                default:
                    output.WriteLine($"Čuvanje nije potvrđeno. API je vratio HTTP {(int)response.StatusCode}.");
                    break;
            }
        }
        catch (HttpRequestException)
        {
            output.WriteLine("Čuvanje nije potvrđeno. Veza sa API-jem nije uspela.");
        }
        catch (OperationCanceledException)
        {
            output.WriteLine("Čuvanje nije potvrđeno. Isteklo je vreme čekanja na API.");
        }
        catch (System.Text.Json.JsonException)
        {
            output.WriteLine("API je prijavio da je nacrt sačuvan, ali odgovor nije čitljiv.");
        }
    }

    private sealed record CreateAssignmentResponse(Guid AssignmentId);
}
