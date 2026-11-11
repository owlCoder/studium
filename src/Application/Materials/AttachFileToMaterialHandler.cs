using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Slučaj upotrebe: vezivanje jedne datoteke za materijal (ST-009, US-06).
/// Poslovno pravilo: prilog se vezuje za materijal tek kada je datoteka uspešno sačuvana.
/// Redosled je zato uvek: provera ulaza i materijala → čuvanje sadržaja u skladištu →
/// tek onda zapis priloga. Ako čuvanje ne uspe, izuzetak se prosleđuje i zapis se ne pravi.
/// Ako čuvanje uspe, a upis zapisa ne uspe, sačuvani sadržaj se briše (US-06, tačka 2).
/// Ograničenje tipa i veličine (US-05): nedozvoljen tip se odbija pre čuvanja, a veličina
/// se proverava na stvarno pročitanim bajtovima, pa prevelika datoteka ne ostaje u skladištu.
/// NAPOMENA: provera pripadnosti nastavnom osoblju izvođenja (ST-005) i prijava rezultata
/// po datoteci za više datoteka (US-06, tačka 3) nisu deo ovog koraka.
/// </summary>
public sealed class AttachFileToMaterialHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IAttachmentFileStorage _storage;
    private readonly AttachmentUploadPolicy _policy;

    public AttachFileToMaterialHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IAttachmentFileStorage storage,
        AttachmentUploadPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(policy);

        _materials = materials;
        _attachments = attachments;
        _storage = storage;
        _policy = policy;
    }

    /// <summary>
    /// Vraća identifikator novog priloga ili <c>null</c> ako materijal ne postoji
    /// (tada se sadržaj ne čuva). Nevažeći naziv ili tip izazivaju
    /// <see cref="ArgumentException"/> pre bilo kakvog upisa u skladište.
    /// Datoteka nedozvoljenog tipa ili prevelika datoteka izaziva
    /// <see cref="AttachmentRejectedException"/>; tada nema ni zapisa ni sačuvanog sadržaja.
    /// </summary>
    public async Task<Guid?> HandleAsync(
        AttachFileToMaterialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content, nameof(command.Content));

        // Validacija pre čuvanja, da nevažeći zahtev ne ostavi sadržaj u skladištu.
        if (string.IsNullOrWhiteSpace(command.OriginalFileName))
            throw new ArgumentException("Izvorni naziv priloga je obavezan.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.ContentType))
            throw new ArgumentException("Tip priloga je obavezan.", nameof(command));

        // US-05: tip i (kada je poznata unapred) veličina proveravaju se pre čuvanja.
        if (!_policy.IsAllowedFileName(command.OriginalFileName))
            throw Rejected(AttachmentRejectionReason.UnsupportedType, command.OriginalFileName);
        if (command.Content.CanSeek &&
            !_policy.IsWithinMaxSize(command.Content.Length - command.Content.Position))
            throw Rejected(AttachmentRejectionReason.TooLarge, command.OriginalFileName);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return null;

        // Veličina se meri na stvarno sačuvanim bajtovima, ne na deklarisanoj vrednosti.
        // Čim pročitani sadržaj pređe maksimum, čitanje se prekida (US-05); skladište tada
        // ne zadržava delimičnu datoteku, a zapis priloga se ne pravi.
        var counting = new ByteCountingStream(
            command.Content,
            _policy.MaxSizeInBytes,
            () => Rejected(AttachmentRejectionReason.TooLarge, command.OriginalFileName));
        var storageKey = await _storage.SaveAsync(counting, cancellationToken);

        // Do ove tačke se dolazi samo ako je skladište potvrdilo čuvanje (vratilo ključ).
        try
        {
            var attachment = new MaterialAttachment(
                Guid.NewGuid(),
                material.Id,
                command.OriginalFileName,
                command.ContentType,
                counting.BytesRead,
                storageKey);

            await _attachments.AddAsync(attachment, cancellationToken);

            return attachment.Id;
        }
        catch
        {
            // Neuspela operacija ne sme delimično promeniti stanje (docs/architecture.md):
            // sadržaj bez zapisa priloga se uklanja, a prosleđuje se izvorna greška.
            await TryDeleteStoredContentAsync(storageKey);
            throw;
        }
    }

    private AttachmentRejectedException Rejected(AttachmentRejectionReason reason, string fileName)
    {
        var name = fileName.Trim();
        var message = reason == AttachmentRejectionReason.UnsupportedType
            ? $"Tip datoteke '{name}' nije dozvoljen."
            : $"Datoteka '{name}' prelazi maksimalnu veličinu od {_policy.MaxSizeInBytes} bajtova.";
        return new AttachmentRejectedException(reason, name, message);
    }

    private async Task TryDeleteStoredContentAsync(string storageKey)
    {
        try
        {
            // Bez tokena otkazivanja: čišćenje mora da se izvrši i kada je zahtev otkazan.
            await _storage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception)
        {
            // Greška čišćenja ne sme da sakrije izvornu grešku. Zaostali sadržaj nije
            // prilog (nema zapisa), pa ga korisnik ne vidi niti može da preuzme.
        }
    }

    /// <summary>
    /// Omotač toka samo za čitanje koji broji pročitane bajtove i prekida čitanje
    /// izuzetkom čim broj pročitanih bajtova pređe dozvoljeni maksimum.
    /// </summary>
    private sealed class ByteCountingStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private readonly Func<Exception> _onLimitExceeded;

        public ByteCountingStream(Stream inner, long maxBytes, Func<Exception> onLimitExceeded)
        {
            _inner = inner;
            _maxBytes = maxBytes;
            _onLimitExceeded = onLimitExceeded;
        }

        public long BytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Count(_inner.Read(buffer, offset, count));
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return Count(await _inner.ReadAsync(buffer, cancellationToken));
        }

        private int Count(int read)
        {
            BytesRead += read;
            if (BytesRead > _maxBytes)
                throw _onLimitExceeded();
            return read;
        }

        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
