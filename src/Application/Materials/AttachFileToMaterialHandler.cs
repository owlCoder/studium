using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Slučaj upotrebe: vezivanje jedne datoteke za materijal (ST-009, US-06).
/// Poslovno pravilo: prilog se vezuje za materijal tek kada je datoteka uspešno sačuvana.
/// Redosled je zato uvek: provera ulaza i materijala → čuvanje sadržaja u skladištu →
/// tek onda zapis priloga. Ako čuvanje ne uspe, izuzetak se prosleđuje i zapis se ne pravi.
/// Ako čuvanje uspe, a upis zapisa ne uspe, sačuvani sadržaj se briše (US-06, tačka 2).
/// NAPOMENA: provera pripadnosti nastavnom osoblju izvođenja (ST-005), ograničenje tipa
/// i veličine (US-05) i prijava rezultata po datoteci za više datoteka (US-06, tačka 3)
/// nisu deo ovog koraka.
/// </summary>
public sealed class AttachFileToMaterialHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IAttachmentFileStorage _storage;

    public AttachFileToMaterialHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IAttachmentFileStorage storage)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(storage);

        _materials = materials;
        _attachments = attachments;
        _storage = storage;
    }

    /// <summary>
    /// Vraća identifikator novog priloga ili <c>null</c> ako materijal ne postoji
    /// (tada se sadržaj ne čuva). Nevažeći naziv ili tip izazivaju
    /// <see cref="ArgumentException"/> pre bilo kakvog upisa u skladište.
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

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return null;

        // Veličina se meri na stvarno sačuvanim bajtovima, ne na deklarisanoj vrednosti.
        var counting = new ByteCountingStream(command.Content);
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
    /// Omotač toka samo za čitanje koji broji pročitane bajtove.
    /// </summary>
    private sealed class ByteCountingStream : Stream
    {
        private readonly Stream _inner;

        public ByteCountingStream(Stream inner) => _inner = inner;

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
            var read = _inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
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
