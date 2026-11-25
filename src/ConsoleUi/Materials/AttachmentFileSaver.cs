namespace ConsoleUi.Materials;

/// <summary>
/// Čuva preuzeti prilog u izabranom direktorijumu pod izvornim nazivom (ST-009, US-11, stavka 3).
/// Izvorni naziv dolazi od nastavnika, pa se pre upisa svodi na bezbedno ime datoteke:
/// putanja se odbacuje (datoteka ne može da izađe iz direktorijuma), a znakovi koji nisu
/// dozvoljeni u imenu datoteke menjaju se sa <c>_</c>. Postojeća datoteka se nikad ne prepisuje:
/// novi prilog dobija ime <c>naziv (2).ext</c>, <c>naziv (3).ext</c>…, kao u pregledačima.
/// </summary>
public sealed class AttachmentFileSaver
{
    private const string FallbackFileName = "prilog";

    // Isti skup na svakom operativnom sistemu, da se naziv ponaša isto lokalno (Windows) i u CI (Linux).
    private static readonly char[] ForbiddenFileNameChars =
        Path.GetInvalidFileNameChars()
            .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
            .Distinct()
            .ToArray();

    private readonly string _targetDirectory;

    public AttachmentFileSaver(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new ArgumentException("Direktorijum za preuzimanje je obavezan.", nameof(targetDirectory));

        _targetDirectory = Path.GetFullPath(targetDirectory);
    }

    /// <summary>
    /// Upisuje sadržaj i vraća punu putanju sačuvane datoteke.
    /// Ako upis ne uspe, ne ostaje delimično zapisana datoteka.
    /// </summary>
    public async Task<string> SaveAsync(
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        Directory.CreateDirectory(_targetDirectory);
        var path = FirstFreePath(ToSafeFileName(originalFileName));

        // CreateNew: ako datoteka sa tim imenom u međuvremenu nastane, upis se odbija umesto da je prepiše.
        var target = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        try
        {
            await using (target)
                await content.CopyToAsync(target, cancellationToken);
        }
        catch
        {
            // Briše se samo datoteka koju je ovaj poziv napravio, nikad tuđa.
            TryDelete(path);
            throw;
        }

        return path;
    }

    /// <summary>
    /// Svodi izvorni naziv na ime datoteke bez putanje i bez nedozvoljenih znakova.
    /// </summary>
    public static string ToSafeFileName(string? originalFileName)
    {
        var withoutPath = (originalFileName ?? string.Empty)
            .Replace('\\', '/')
            .Split('/')
            .Last();

        var safe = new string(withoutPath
            .Select(c => ForbiddenFileNameChars.Contains(c) ? '_' : c)
            .ToArray());

        // Windows ne dozvoljava tačku ili razmak na kraju imena; "." i ".." nisu imena datoteka.
        safe = safe.Trim().TrimEnd('.', ' ');

        return safe.Length == 0 ? FallbackFileName : safe;
    }

    private string FirstFreePath(string fileName)
    {
        var path = Path.Combine(_targetDirectory, fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var copy = 2; File.Exists(path); copy++)
            path = Path.Combine(_targetDirectory, $"{name} ({copy}){extension}");

        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Čišćenje nije uspelo; izvorna greška se i dalje prosleđuje pozivaocu.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
