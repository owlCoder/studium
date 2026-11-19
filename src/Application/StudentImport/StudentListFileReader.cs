using System.Buffers;
using System.Text;

namespace Application.StudentImport;

/// <summary>Уговор за читање улазне датотеке у текст.</summary>
public interface IStudentListFileReader
{
    Task<string> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
}

/// <summary>Чита текст без замене неисправних бајтова, да се имена не би оштетила.</summary>
/// <remarks>Користи BOM за UTF-8 или UTF-16; без BOM-а прихвата само исправан UTF-8.</remarks>
public sealed class StudentListFileReader : IStudentListFileReader
{
    /// <summary>Ограничење величине улазне датотеке.</summary>
    public const int MaxBytes = 8 * 1024 * 1024;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UnicodeEncoding StrictUtf16Le = new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
    private static readonly UnicodeEncoding StrictUtf16Be = new(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];
    private static ReadOnlySpan<byte> Utf16LeBom => [0xFF, 0xFE];
    private static ReadOnlySpan<byte> Utf16BeBom => [0xFE, 0xFF];

    /// <exception cref="ImportFileRejectedException">
    /// Датотека је превелика, бинарна или се не може прочитати као текст.
    /// </exception>
    public async Task<string> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var capacity = stream.CanSeek && stream.Length > 0 && stream.Length <= MaxBytes
            ? (int)stream.Length
            : 4096;

        using var buffer = new MemoryStream(capacity);
        var chunk = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(chunk, cancellationToken);
                if (read == 0) break;

                if (buffer.Length + read > MaxBytes)
                {
                    throw new ImportFileRejectedException(
                        "Datoteka je prevelika za uvoz; podelite je na vise datoteka.");
                }

                buffer.Write(chunk, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        if (buffer.Length == 0)
            throw new ImportFileRejectedException("Datoteka je prazna.");

        return DecodeBuffer(buffer);
    }

    private string DecodeBuffer(MemoryStream buffer)
    {
        var length = (int)buffer.Length;
        ReadOnlySpan<byte> bytes = buffer.TryGetBuffer(out var segment)
            ? segment.AsSpan(0, length)
            : buffer.ToArray();

        return Decode(bytes);
    }

    public string Decode(byte[] bytes) => Decode(bytes.AsSpan());

    public string Decode(ReadOnlySpan<byte> bytes)
    {
        // Ознака BOM одређује кодирање датотеке.
        if (bytes.StartsWith(Utf8Bom))
            return DecodeStrict(bytes[3..], StrictUtf8, "UTF-8");

        if (bytes.StartsWith(Utf16LeBom))
            return DecodeStrict(bytes[2..], StrictUtf16Le, "UTF-16 LE");

        if (bytes.StartsWith(Utf16BeBom))
            return DecodeStrict(bytes[2..], StrictUtf16Be, "UTF-16 BE");

        // Без BOM-а нулти бајт значи бинарни садржај, не текст.
        if (bytes.IndexOf((byte)0) >= 0)
        {
            throw new ImportFileRejectedException(
                "Datoteka nije tekstualna; ocekuje se CSV u UTF-8 zapisu.");
        }

        // Строг UTF-8: неисправан бајт се одбија, не замењује знаком „�".
        return DecodeStrict(bytes, StrictUtf8, "UTF-8");
    }

    private static string DecodeStrict(ReadOnlySpan<byte> bytes, Encoding encoding, string name)
    {
        try
        {
            return encoding.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new ImportFileRejectedException(
                $"Datoteka nije ispravno sacuvana kao {name}. Sacuvajte je ponovo u UTF-8 zapisu.");
        }
    }
}
