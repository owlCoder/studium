using System.Globalization;

namespace ConsoleUi.Materials;

/// <summary>
/// Pretvara veličinu datoteke u bajtovima u čitljiv tekst za korisnika
/// (npr. <c>7 B</c>, <c>2 KB</c>, <c>1.5 MB</c>), gde je 1 KB = 1024 B i 1 MB = 1024 KB.
/// Pravilo je isto kao u listi priloga za nastavnika (<see cref="UploadAttachmentsForm"/>),
/// da nastavnik i student vide istu veličinu istog priloga.
/// </summary>
public static class FileSizeFormatter
{
    private const long Kilobyte = 1024;
    private const long Megabyte = Kilobyte * 1024;

    public static string Format(long sizeInBytes)
    {
        if (sizeInBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Veličina datoteke ne može biti negativna.");

        if (sizeInBytes >= Megabyte)
            return WithOneDecimal(sizeInBytes, Megabyte) + " MB";

        if (sizeInBytes >= Kilobyte)
            return WithOneDecimal(sizeInBytes, Kilobyte) + " KB";

        return sizeInBytes.ToString(CultureInfo.InvariantCulture) + " B";
    }

    private static string WithOneDecimal(long sizeInBytes, long unit) =>
        (sizeInBytes / (double)unit).ToString("0.#", CultureInfo.InvariantCulture);
}
