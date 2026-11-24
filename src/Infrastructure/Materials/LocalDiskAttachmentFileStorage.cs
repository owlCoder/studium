using Application.Materials;

namespace Infrastructure.Materials;

/// <summary>
/// Skladištenje priloga na lokalnom disku, u zadatom korenskom direktorijumu.
/// Sadržaj se prvo upisuje u privremenu datoteku, a tek po uspešnom upisu
/// premešta pod konačni ključ, tako da neuspeo upis ne ostavlja delimičnu datoteku.
/// </summary>
public sealed class LocalDiskAttachmentFileStorage : IAttachmentFileStorage
{
    private const string TempExtension = ".tmp";

    private readonly string _rootPath;

    public LocalDiskAttachmentFileStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Korenski direktorijum skladišta je obavezan.", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        var storageKey = Guid.NewGuid().ToString("N");
        var finalPath = Path.Combine(_rootPath, storageKey);
        var tempPath = finalPath + TempExtension;

        try
        {
            await using (var target = new FileStream(
                tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(target, cancellationToken);
                await target.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, finalPath);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsValidKey(storageKey))
        {
            return Task.FromResult<Stream?>(null);
        }

        var path = Path.Combine(_rootPath, storageKey);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsValidKey(storageKey))
        {
            return Task.FromResult(false);
        }

        var path = Path.Combine(_rootPath, storageKey);
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    // Ključ mora biti tačno u formatu koji skladište generiše (32 heksadecimalna znaka),
    // čime se sprečava pristup putanjama van korenskog direktorijuma (npr. "..\\").
    private static bool IsValidKey(string? storageKey) =>
        storageKey is { Length: 32 } && Guid.TryParseExact(storageKey, "N", out _);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Privremena datoteka se ne prijavljuje kao prilog; čišćenje nije kritično.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
