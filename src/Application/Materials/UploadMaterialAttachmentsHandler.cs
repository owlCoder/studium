namespace Application.Materials;

/// <summary>
/// Slučaj upotrebe: nastavnik otprema jednu ili više datoteka u materijal (ST-009, US-04).
/// Redosled:
/// 1. materijal mora postojati (inače <c>null</c>);
/// 2. korisnik mora pripadati nastavnom osoblju izvođenja materijala (ST-005, CONTRIBUTING.md);
/// 3. sve datoteke se proveravaju unapred (naziv, tip, prijavljena veličina, US-05),
///    pa ako je bilo koja neispravna, ne čuva se nijedna;
/// 4. datoteke se čuvaju redom preko <see cref="AttachFileToMaterialHandler"/> (US-06),
///    koji vezuje prilog tek posle uspešnog čuvanja i meri stvarnu veličinu.
/// Ako čuvanje neke datoteke ipak ne uspe u koraku 4, greška se prosleđuje za tu datoteku,
/// a prethodno sačuvane datoteke ostaju priložene. Detaljan izveštaj po datoteci
/// (sačuvano / nije sačuvano) je deo US-06 i nije u ovom koraku.
/// </summary>
public sealed class UploadMaterialAttachmentsHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IOfferingStaffChecker _staffChecker;
    private readonly AttachmentUploadPolicy _policy;
    private readonly AttachFileToMaterialHandler _attachFile;

    public UploadMaterialAttachmentsHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IOfferingStaffChecker staffChecker,
        AttachmentUploadPolicy policy,
        AttachFileToMaterialHandler attachFile)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(staffChecker);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(attachFile);

        _materials = materials;
        _attachments = attachments;
        _staffChecker = staffChecker;
        _policy = policy;
        _attachFile = attachFile;
    }

    /// <returns>
    /// Sačuvani prilozi, redom kojim su datoteke poslate, ili <c>null</c> ako materijal ne postoji.
    /// </returns>
    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    /// <exception cref="ArgumentException">Nije poslata nijedna datoteka ili datoteka nema naziv/tip.</exception>
    /// <exception cref="AttachmentRejectedException">Datoteka nedozvoljenog tipa ili prevelika datoteka.</exception>
    public async Task<IReadOnlyList<UploadedAttachment>?> HandleAsync(
        UploadMaterialAttachmentsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return null;

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može prilagati datoteke materijalu.");
        }

        if (command.Files is null || command.Files.Count == 0)
            throw new ArgumentException("Potrebno je poslati bar jednu datoteku.", "files");

        foreach (var file in command.Files)
        {
            ValidateBeforeSaving(file);
        }

        var savedIds = new List<Guid>(command.Files.Count);
        foreach (var file in command.Files)
        {
            await using var content = file.OpenReadStream();

            var id = await _attachFile.HandleAsync(
                new AttachFileToMaterialCommand(material.Id, file.FileName, file.ContentType, content),
                cancellationToken);

            if (id is null)
                return null; // Materijal je u međuvremenu uklonjen.

            savedIds.Add(id.Value);
        }

        var stored = (await _attachments.ListByMaterialAsync(material.Id, cancellationToken))
            .ToDictionary(a => a.Id);

        return savedIds
            .Select(id => stored[id])
            .Select(a => new UploadedAttachment(a.Id, a.OriginalFileName, a.ContentType, a.SizeInBytes))
            .ToArray();
    }

    // Provera pre čuvanja bilo koje datoteke, da odbijena datoteka ne ostavi ostale delimično sačuvane.
    // Poruke prate format dokumentovan u ADR-0002.
    private void ValidateBeforeSaving(UploadedFile? file)
    {
        if (file is null)
            throw new ArgumentException("Datoteka nije zadata.", "files");
        if (string.IsNullOrWhiteSpace(file.FileName))
            throw new ArgumentException("Izvorni naziv priloga je obavezan.", "files");
        if (string.IsNullOrWhiteSpace(file.ContentType))
            throw new ArgumentException($"Tip priloga '{file.FileName.Trim()}' je obavezan.", "files");

        var name = file.FileName.Trim();

        if (!_policy.IsAllowedFileName(name))
        {
            throw new AttachmentRejectedException(
                AttachmentRejectionReason.UnsupportedType,
                name,
                $"Datoteka '{name}' nije priložena: tip datoteke nije dozvoljen. " +
                $"Dozvoljeni tipovi: {_policy.AllowedExtensionsDisplay}.");
        }

        if (!_policy.IsWithinMaxSize(file.Length))
        {
            throw new AttachmentRejectedException(
                AttachmentRejectionReason.TooLarge,
                name,
                $"Datoteka '{name}' nije priložena: veća je od dozvoljenih {_policy.MaxSizeDisplay}.");
        }
    }
}
