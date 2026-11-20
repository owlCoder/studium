namespace Domain.Submissions;

/// <summary>
/// Prilog uz predaju individualnog rada (ST-012).
/// Za sada se čuvaju samo podaci o fajlu (naziv, tip, veličina), ne sam fajl.
/// </summary>
public class Attachment
{
    public string FileName { get; }
    public string FileType { get; }
    public long Size { get; }

    public Attachment(string fileName, string fileType, long size)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Naziv priloga je obavezan.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileType))
            throw new ArgumentException("Tip priloga je obavezan.", nameof(fileType));
        if (size <= 0)
            throw new ArgumentException("Prilog ne može biti prazan.", nameof(size));

        FileName = fileName.Trim();
        FileType = fileType.Trim();
        Size = size;
    }
}