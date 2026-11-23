using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Demo;

namespace ConsoleUi.Scenarios;

public static class ViewOwnSubmissionScenario
{
    public static async Task RunAsync(HttpClient client, TextReader input, TextWriter output)
    {
        output.WriteLine("STUDIUM | Pregled sopstvene predaje (ST-012, demo student)");
        output.WriteLine($"ID zadatka za demo: {SubmissionDemoData.AssignmentId}");
        output.Write("Unesi ID zadatka ili ostavi prazno za izlaz: ");
        var value = input.ReadLine();
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!Guid.TryParse(value, out var assignmentId) || assignmentId == Guid.Empty)
        {
            output.WriteLine("Identifikator zadatka nije ispravan.");
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/assignments/{assignmentId}/submissions/me");
            request.Headers.Add("X-Demo-UserId", SubmissionDemoData.StudentId.ToString());
            using var response = await client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var submission = await response.Content.ReadFromJsonAsync<SubmissionResponse>();
                    if (submission == null)
                        output.WriteLine("API je vratio prazan odgovor.");
                    else
                        PrintSubmission(submission, output);
                    break;
                case HttpStatusCode.NotFound:
                    output.WriteLine("Nemaš predaju za ovaj zadatak.");
                    break;
                case HttpStatusCode.Unauthorized:
                    output.WriteLine("Demo prijava nije prihvaćena. Proveri okruženje API-ja.");
                    break;
                case HttpStatusCode.BadRequest:
                    output.WriteLine("API je odbio zahtev za pregled predaje.");
                    break;
                default:
                    output.WriteLine($"Pregled nije uspeo. API je vratio HTTP {(int)response.StatusCode}.");
                    break;
            }
        }
        catch (HttpRequestException)
        {
            output.WriteLine("Veza sa API-jem nije uspela.");
        }
        catch (OperationCanceledException)
        {
            output.WriteLine("Isteklo je vreme čekanja na API.");
        }
        catch (JsonException)
        {
            output.WriteLine("Odgovor API-ja nije čitljiv.");
        }
    }

    private static void PrintSubmission(SubmissionResponse submission, TextWriter output)
    {
        output.WriteLine($"Predato: {submission.SubmittedAt.ToString("dd.MM.yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
        output.WriteLine($"Tekst: {submission.Text}");
        output.WriteLine("Prilozi:");
        if (submission.Attachments.Count == 0)
            output.WriteLine("Nema priloga.");

        foreach (var attachment in submission.Attachments)
            output.WriteLine($"- {attachment.FileName} ({attachment.FileType}, {attachment.Size} B)");
    }

    private sealed record SubmissionResponse(string Text, DateTimeOffset SubmittedAt, List<AttachmentResponse> Attachments);
    private sealed record AttachmentResponse(string FileName, string FileType, long Size);
}
