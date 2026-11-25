using Application.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolno preuzimanje priloga za studenta (ST-009, US-11, stavka 3): datoteka se čuva
/// pod izvornim nazivom u izabranom direktorijumu.
/// Ko sme da preuzme prilog (aktivan student, objavljen materijal, prilog pripada materijalu)
/// određuje <see cref="DownloadMaterialAttachmentHandler"/> iz priče o zaštiti pristupa (US-12),
/// a čuvanje na disk <see cref="AttachmentFileSaver"/>. Ovaj prikaz samo povezuje ta dva
/// koraka i saopštava ishod korisniku.
/// </summary>
public sealed class AttachmentDownloadView
{
    private readonly DownloadMaterialAttachmentHandler _handler;
    private readonly AttachmentFileSaver _saver;
    private readonly TextWriter _output;

    public AttachmentDownloadView(
        DownloadMaterialAttachmentHandler handler,
        AttachmentFileSaver saver,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(saver);
        ArgumentNullException.ThrowIfNull(output);

        _handler = handler;
        _saver = saver;
        _output = output;
    }

    /// <summary>
    /// Preuzima prilog i vraća putanju sačuvane datoteke, ili <c>null</c> ako prilog nije dostupan.
    /// </summary>
    public async Task<string?> DownloadAsync(
        Guid requestingUserId,
        Guid materialId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        DownloadMaterialAttachmentResult result;
        try
        {
            result = await _handler.HandleAsync(
                new DownloadMaterialAttachmentCommand(requestingUserId, materialId, attachmentId),
                cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Prilog nije dostupan: {ex.Message}");
            return null;
        }

        // Nepostojeći materijal ili prilog, nacrt i nedostajući sadržaj daju istu poruku,
        // kao što i API za sve njih vraća 404.
        if (result.Outcome != DownloadMaterialAttachmentOutcome.Found
            || result.Attachment is null
            || result.Content is null)
        {
            _output.WriteLine("Prilog nije dostupan.");
            return null;
        }

        string savedPath;
        await using (result.Content)
            savedPath = await _saver.SaveAsync(result.Attachment.OriginalFileName, result.Content, cancellationToken);

        var attachment = result.Attachment;
        _output.WriteLine(
            $"Preuzeto: {attachment.OriginalFileName} ({attachment.ContentType}, {FileSizeFormatter.Format(attachment.SizeInBytes)})");
        _output.WriteLine($"Sačuvano u: {savedPath}");
        return savedPath;
    }
}
