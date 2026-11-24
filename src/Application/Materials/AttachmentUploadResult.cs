namespace Application.Materials;

/// <summary>
/// Rezultat otpremanja jedne datoteke (ST-009, US-06): sačuvano / nije sačuvano.
/// Nastavnik iz rezultata tačno vidi koji su prilozi sačuvani, kako ne bi mislio
/// da je materijal kompletan kada nije.
/// </summary>
/// <param name="OriginalFileName">Naziv datoteke kakav je poslat.</param>
/// <param name="Saved"><c>true</c> samo ako je datoteka sačuvana i vezana za materijal.</param>
/// <param name="Message">Poruka za korisnika o ishodu za ovu datoteku.</param>
/// <param name="Attachment">Sačuvani prilog; <c>null</c> kada datoteka nije sačuvana.</param>
public sealed record AttachmentUploadResult(
    string OriginalFileName,
    bool Saved,
    string Message,
    UploadedAttachment? Attachment)
{
    public static AttachmentUploadResult Success(UploadedAttachment attachment) =>
        new(attachment.OriginalFileName, true,
            $"Datoteka '{attachment.OriginalFileName}' je sačuvana.", attachment);

    public static AttachmentUploadResult Failure(string originalFileName, string message) =>
        new(originalFileName, false, message, null);
}
