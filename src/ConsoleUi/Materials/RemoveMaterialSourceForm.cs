using Application.Materials;
using Domain.Materials;

namespace ConsoleUi.Materials;

/// <summary>
/// Konzolna akcija „Ukloni“ sa potvrdom za prilog ili spoljnu vezu materijala (ST-009, US-07, tačka 3).
/// Prikazuje numerisanu listu priloga i veza; nastavnik bira broj, a forma traži izričitu potvrdu.
/// Podrazumevani odgovor je „ne“, pa prazan unos, kraj ulaza ili bilo šta osim „d“/„da“ otkazuje
/// uklanjanje bez promene (isti obrazac kao <see cref="WithdrawMaterialForm"/>).
/// Ovo je prezentacija, ne bezbednosna kontrola: pravo uklanjanja (nastavno osoblje izvođenja,
/// ST-005) i pripadnost priloga ili veze materijalu proveravaju
/// <see cref="RemoveMaterialAttachmentHandler"/> i <see cref="RemoveMaterialExternalLinkHandler"/>.
/// </summary>
public sealed class RemoveMaterialSourceForm
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IMaterialExternalLinkRepository _links;
    private readonly RemoveMaterialAttachmentHandler _removeAttachment;
    private readonly RemoveMaterialExternalLinkHandler _removeLink;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public RemoveMaterialSourceForm(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IMaterialExternalLinkRepository links,
        RemoveMaterialAttachmentHandler removeAttachment,
        RemoveMaterialExternalLinkHandler removeLink,
        TextReader input,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(removeAttachment);
        ArgumentNullException.ThrowIfNull(removeLink);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _materials = materials;
        _attachments = attachments;
        _links = links;
        _removeAttachment = removeAttachment;
        _removeLink = removeLink;
        _input = input;
        _output = output;
    }

    /// <summary>
    /// Vraća <c>true</c> samo ako je izabrani prilog ili veza uklonjena.
    /// </summary>
    public async Task<bool> RunAsync(
        Guid requestingUserId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        var material = await _materials.GetByIdAsync(materialId, cancellationToken);
        if (material is null)
        {
            _output.WriteLine("Materijal nije pronađen.");
            return false;
        }

        _output.WriteLine($"Uklanjanje priloga i veza: {material.Name}");

        var sources = await LoadSourcesAsync(materialId, cancellationToken);
        if (sources.Count == 0)
        {
            _output.WriteLine("Materijal nema priloga ni spoljnih veza.");
            return false;
        }

        WriteSources(sources);

        var selected = ReadSelection(sources);
        if (selected is null)
        {
            _output.WriteLine("Ništa nije izabrano. Ništa nije uklonjeno.");
            return false;
        }

        _output.Write(selected.ConfirmationQuestion + " (d/N): ");
        if (!IsConfirmed(_input.ReadLine()))
        {
            _output.WriteLine("Uklanjanje je otkazano. Ništa nije uklonjeno.");
            return false;
        }

        bool removed;
        try
        {
            removed = selected.IsAttachment
                ? await RemoveAttachmentAsync(requestingUserId, materialId, selected, cancellationToken)
                : await RemoveLinkAsync(requestingUserId, materialId, selected, cancellationToken);
        }
        catch (MaterialAccessDeniedException ex)
        {
            _output.WriteLine($"Ništa nije uklonjeno: {ex.Message}");
            return false;
        }

        if (removed)
        {
            var remaining = await LoadSourcesAsync(materialId, cancellationToken);
            _output.WriteLine();
            if (remaining.Count == 0)
                _output.WriteLine("Materijal više nema priloga ni spoljnih veza.");
            else
                WriteSources(remaining);
        }

        return removed;
    }

    private async Task<bool> RemoveAttachmentAsync(
        Guid userId, Guid materialId, Source source, CancellationToken cancellationToken)
    {
        var outcome = await _removeAttachment.HandleAsync(
            new RemoveMaterialAttachmentCommand(userId, materialId, source.Id), cancellationToken);

        switch (outcome)
        {
            case RemoveMaterialAttachmentOutcome.Removed:
                _output.WriteLine($"Prilog '{source.Label}' je uklonjen.");
                return true;
            case RemoveMaterialAttachmentOutcome.AttachmentNotFound:
                _output.WriteLine($"Prilog '{source.Label}' više ne postoji; možda je već uklonjen.");
                return false;
            default:
                _output.WriteLine("Materijal nije pronađen.");
                return false;
        }
    }

    private async Task<bool> RemoveLinkAsync(
        Guid userId, Guid materialId, Source source, CancellationToken cancellationToken)
    {
        var outcome = await _removeLink.HandleAsync(
            new RemoveMaterialExternalLinkCommand(userId, materialId, source.Id), cancellationToken);

        switch (outcome)
        {
            case RemoveMaterialExternalLinkOutcome.Removed:
                _output.WriteLine($"Veza '{source.Label}' je uklonjena.");
                return true;
            case RemoveMaterialExternalLinkOutcome.LinkNotFound:
                _output.WriteLine($"Veza '{source.Label}' više ne postoji; možda je već uklonjena.");
                return false;
            default:
                _output.WriteLine("Materijal nije pronađen.");
                return false;
        }
    }

    // Prvo prilozi, pa veze; obe grupe azbučno, da bi brojevi bili stabilni za isti sadržaj.
    private async Task<IReadOnlyList<Source>> LoadSourcesAsync(Guid materialId, CancellationToken cancellationToken)
    {
        var attachments = (await _attachments.ListByMaterialAsync(materialId, cancellationToken))
            .OrderBy(a => a.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Id)
            .Select(a => Source.ForAttachment(a));

        var links = (await _links.ListByMaterialAsync(materialId, cancellationToken))
            .OrderBy(l => l.Url.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Id)
            .Select(l => Source.ForLink(l));

        return attachments.Concat(links).ToArray();
    }

    private void WriteSources(IReadOnlyList<Source> sources)
    {
        _output.WriteLine($"Prilozi i veze ({sources.Count}):");
        for (var i = 0; i < sources.Count; i++)
            _output.WriteLine($"  {i + 1}. {sources[i].Description}");
    }

    // Ponavlja pitanje dok broj nije ispravan; prazan unos ili kraj ulaza znače da ništa nije izabrano.
    private Source? ReadSelection(IReadOnlyList<Source> sources)
    {
        while (true)
        {
            _output.Write($"Broj za uklanjanje (1–{sources.Count}, Enter = ništa): ");
            var line = _input.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
                return null;

            if (int.TryParse(line.Trim(), out var number) && number >= 1 && number <= sources.Count)
                return sources[number - 1];

            _output.WriteLine($"Unesite broj od 1 do {sources.Count}.");
        }
    }

    private static bool IsConfirmed(string? answer) =>
        answer?.Trim().ToLowerInvariant() is "d" or "da";

    private sealed record Source(
        Guid Id,
        bool IsAttachment,
        string Label,
        string Description,
        string ConfirmationQuestion)
    {
        public static Source ForAttachment(MaterialAttachment attachment) => new(
            attachment.Id,
            IsAttachment: true,
            attachment.OriginalFileName,
            $"[prilog] {attachment.OriginalFileName} " +
            $"({attachment.ContentType}, {FileSizeFormatter.Format(attachment.SizeInBytes)})",
            $"Ukloniti prilog '{attachment.OriginalFileName}'? Datoteka se trajno briše.");

        public static Source ForLink(MaterialExternalLink link) => new(
            link.Id,
            IsAttachment: false,
            link.Url.AbsoluteUri,
            $"[veza] {link.Url.AbsoluteUri}",
            $"Ukloniti vezu '{link.Url.AbsoluteUri}'?");
    }
}
