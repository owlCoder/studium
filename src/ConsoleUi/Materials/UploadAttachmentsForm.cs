using System.Globalization;
using Application.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna forma za izbor i slanje datoteka u materijal, sa prikazom liste priloga (ST-009, US-04).
/// Nastavnik unosi putanje do datoteka, po jednu u redu; prazan red završava izbor.
/// Pre izbora forma prikazuje dozvoljene tipove i maksimalnu veličinu, a datoteku koja
/// ih ne zadovoljava odbija već pri unosu (US-05, provera na klijentu).
/// Provera na klijentu nije bezbednosna kontrola: pravo pristupa (ST-005), tip i veličinu
/// (US-05) i rezultat po datoteci (US-06) i dalje određuje <see cref="UploadMaterialAttachmentsHandler"/>.
/// </summary>
public sealed class UploadAttachmentsForm
{
    // Tip sadržaja za datoteke koje se biraju sa diska; konzola ga ne dobija od pregledača.
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".txt"] = "text/plain",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".zip"] = "application/zip",
        };

    private const string DefaultContentType = "application/octet-stream";

    private readonly UploadMaterialAttachmentsHandler _handler;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly AttachmentUploadPolicy _policy;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public UploadAttachmentsForm(
        UploadMaterialAttachmentsHandler handler,
        IMaterialAttachmentRepository attachments,
        AttachmentUploadPolicy policy,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _handler = handler;
        _attachments = attachments;
        _policy = policy;
        _input = input;
        _output = output;
    }

    /// <summary>
    /// Vraća rezultat po poslatoj datoteci ili <c>null</c> ako ništa nije poslato
    /// (prekinut unos, nijedna izabrana datoteka, odbijen zahtev ili nepostojeći materijal).
    /// </summary>
    public async Task<IReadOnlyList<AttachmentUploadResult>?> RunAsync(
        Guid requestingUserId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        _output.WriteLine("Otpremanje priloga");
        // US-05: nastavnik unapred vidi koje datoteke sme da priloži.
        _output.WriteLine($"Dozvoljeni tipovi: {_policy.AllowedExtensionsDisplay}");
        _output.WriteLine($"Najveća veličina jedne datoteke: {_policy.MaxSizeDisplay}");
        _output.WriteLine("Unesite putanju do datoteke, po jednu u redu. Prazan red završava izbor.");

        var paths = new List<string>();
        while (true)
        {
            _output.Write($"Datoteka {paths.Count + 1}: ");
            var line = _input.ReadLine();

            if (line is null)
            {
                _output.WriteLine();
                _output.WriteLine("Unos je prekinut. Nijedna datoteka nije poslata.");
                return null;
            }

            var path = NormalizePath(line);
            if (path.Length == 0)
                break;

            if (!File.Exists(path))
            {
                _output.WriteLine($"Datoteka '{path}' ne postoji.");
                continue;
            }

            // US-05, provera na klijentu: odbijena datoteka se ne dodaje u izbor,
            // a poruka je ista kao kada je odbije server.
            var rejection = CheckAgainstPolicy(path);
            if (rejection is not null)
            {
                _output.WriteLine(rejection);
                continue;
            }

            paths.Add(path);
        }

        if (paths.Count == 0)
        {
            _output.WriteLine("Nije izabrana nijedna datoteka. Ništa nije poslato.");
            return null;
        }

        var files = paths
            .Select(p => new UploadedFile(
                Path.GetFileName(p),
                ContentTypeFor(p),
                new FileInfo(p).Length,
                () => File.OpenRead(p)))
            .ToArray();

        IReadOnlyList<AttachmentUploadResult>? results;
        try
        {
            results = await _handler.HandleAsync(
                new UploadMaterialAttachmentsCommand(requestingUserId, materialId, files),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Datoteke nisu poslate: {ex.Message}");
            return null;
        }
        catch (AttachmentRejectedException ex)
        {
            _output.WriteLine($"Datoteke nisu poslate. {ex.Message}");
            return null;
        }
        catch (ArgumentException ex)
        {
            _output.WriteLine($"Datoteke nisu poslate: {MessageWithoutParameter(ex)}");
            return null;
        }

        if (results is null)
        {
            _output.WriteLine("Datoteke nisu poslate: materijal ne postoji.");
            return null;
        }

        _output.WriteLine();
        _output.WriteLine("Rezultat otpremanja:");
        foreach (var result in results)
        {
            var status = result.Saved ? "sačuvano" : "nije sačuvano";
            _output.WriteLine($"  [{status}] {result.Message}");
        }

        await WriteAttachmentListAsync(materialId, cancellationToken);
        return results;
    }

    // Lista se prikazuje samo nakon što je slučaj upotrebe proverio pravo pristupa materijalu.
    private async Task WriteAttachmentListAsync(Guid materialId, CancellationToken cancellationToken)
    {
        var attachments = (await _attachments.ListByMaterialAsync(materialId, cancellationToken))
            .OrderBy(a => a.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _output.WriteLine();
        _output.WriteLine($"Prilozi materijala ({attachments.Length}):");

        if (attachments.Length == 0)
        {
            _output.WriteLine("  (nema priloga)");
            return;
        }

        foreach (var attachment in attachments)
        {
            _output.WriteLine(
                $"  - {attachment.OriginalFileName} ({attachment.ContentType}, {FormatSize(attachment.SizeInBytes)})");
        }
    }

    private string? CheckAgainstPolicy(string path)
    {
        var fileName = Path.GetFileName(path);

        if (!_policy.IsAllowedFileName(fileName))
            return _policy.DescribeRejection(AttachmentRejectionReason.UnsupportedType, fileName);

        if (!_policy.IsWithinMaxSize(new FileInfo(path).Length))
            return _policy.DescribeRejection(AttachmentRejectionReason.TooLarge, fileName);

        return null;
    }

    // Putanja kopirana iz Windows Explorer-a ("Copy as path") stiže pod navodnicima.
    private static string NormalizePath(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Trim();
        return trimmed;
    }

    private static string ContentTypeFor(string path) =>
        ContentTypes.TryGetValue(Path.GetExtension(path), out var type) ? type : DefaultContentType;

    private static string MessageWithoutParameter(ArgumentException ex) =>
        ex.ParamName is null
            ? ex.Message
            : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty);

    private static string FormatSize(long bytes)
    {
        const long kilobyte = 1024;
        const long megabyte = kilobyte * 1024;

        if (bytes >= megabyte)
            return (bytes / (double)megabyte).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
        if (bytes >= kilobyte)
            return (bytes / (double)kilobyte).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
        return bytes.ToString(CultureInfo.InvariantCulture) + " B";
    }
}
