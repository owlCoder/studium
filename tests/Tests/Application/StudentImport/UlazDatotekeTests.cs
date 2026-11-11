using System.Text;
using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Провера кодирања, бинарног садржаја и ограничења улазне датотеке.</summary>
[TestFixture]
public sealed class UlazDatotekeTests
{
    private static Stream Bytes(byte[] bytes) => new MemoryStream(bytes);

    private static async Task<string> Read(byte[] bytes)
    {
        using var stream = Bytes(bytes);
        return await new StudentListFileReader().ReadAsync(stream);
    }

    [Test]
    public async Task Utf8_Sa_Bom_Se_Cita()
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            .GetBytes("firstname,lastname,username\nĐorđe,Đorđević,đorđe.đorđević\n");

        bytes = new UTF8Encoding(true).GetPreamble().Concat(bytes).ToArray();
        var text = await Read(bytes);

        Assert.That(text, Does.Contain("Đorđe"));
        Assert.That(text[0], Is.Not.EqualTo('\uFEFF'));
    }

    [Test]
    public async Task Utf8_Bez_Bom_Se_Cita()
    {
        var bytes = new UTF8Encoding(false).GetBytes("Đorđe,Đorđević\n");

        Assert.That(await Read(bytes), Is.EqualTo("Đorđe,Đorđević\n"));
    }

    [Test]
    public async Task Utf16_Le_Sa_Bom_Se_Cita()
    {
        var bytes = new UnicodeEncoding(bigEndian: false, byteOrderMark: true)
            .GetPreamble()
            .Concat(new UnicodeEncoding(false, false).GetBytes("Đorđe,Đorđević\n"))
            .ToArray();

        Assert.That(await Read(bytes), Is.EqualTo("Đorđe,Đorđević\n"));
    }

    [Test]
    public async Task Utf16_Be_Sa_Bom_Se_Cita()
    {
        var bytes = new UnicodeEncoding(bigEndian: true, byteOrderMark: true)
            .GetPreamble()
            .Concat(new UnicodeEncoding(true, false).GetBytes("Đorđe,Đorđević\n"))
            .ToArray();

        Assert.That(await Read(bytes), Is.EqualTo("Đorđe,Đorđević\n"));
    }

    /// <summary>Датотека у кодирању Windows-1250 без ознаке се одбија као неисправан UTF-8.</summary>
    [Test]
    public void Windows_1250_Sa_Dijakritikom_Se_Odbija_Uz_Razlog()
    {
        // Низ у кодирању Windows-1250 није исправан UTF-8.
        var bytes = Encoding.ASCII.GetBytes("firstname,lastname\n")
            .Concat(new byte[] { 0xD0, 0x6F, 0x72, 0xF0, 0x65 })
            .Concat(Encoding.ASCII.GetBytes(",Anic\n"))
            .ToArray();

        var error = Assert.ThrowsAsync<ImportFileRejectedException>(
            async () => await Read(bytes));

        Assert.That(error!.Message, Does.Contain("UTF-8"));
    }

    [Test]
    public void Binarni_Sadrzaj_Se_Odbija()
    {
        var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x08, 0x00 };

        var error = Assert.ThrowsAsync<ImportFileRejectedException>(
            async () => await Read(bytes));

        Assert.That(error!.Message, Does.Contain("nije tekstualna"));
    }

    [Test]
    public void Prazna_Datoteka_Se_Odbija()
    {
        Assert.ThrowsAsync<ImportFileRejectedException>(
            async () => await Read(Array.Empty<byte>()));
    }

    [Test]
    public void Prevelika_Datoteka_Se_Odbija_Bez_Ucitavanja_Celine()
    {
        using var stream = new CountingInput(StudentListFileReader.MaxBytes * 3);
        var error = Assert.ThrowsAsync<ImportFileRejectedException>(async () =>
            await new StudentListFileReader().ReadAsync(stream));
        Assert.That(error!.Message, Does.Contain("prevelika"));
        Assert.That(stream.BytesRead, Is.GreaterThan(StudentListFileReader.MaxBytes));
        Assert.That(stream.BytesRead, Is.LessThanOrEqualTo(StudentListFileReader.MaxBytes + 8192));
        Assert.That(stream.BytesRead, Is.LessThan(stream.TotalBytes));
    }



    [Test]
    public void Predugacko_Polje_Se_Odbija()
    {
        var parser = new StudentListCsvParser();
        var atLimit = new string('a', CsvReader.MaxFieldLength);
        var valid = parser.Parse($"firstname,lastname,username\n\"{atLimit}\",Anic,ana.anic\n");
        Assert.That(valid.Rows.Single().FirstName, Is.EqualTo(atLimit));
        Assert.That(() => parser.Parse($"firstname,lastname,username\n\"{atLimit}a\",Anic,ana.anic\n"),
            Throws.TypeOf<ImportFileRejectedException>().With.Message.Contains("predugacko"));
    }

    [Test]
    public void Predugacko_Necitirano_Polje_Se_Odbija()
    {
        var huge = new string('a', CsvReader.MaxFieldLength + 1);

        Assert.That(
            () => new StudentListCsvParser().Parse($"firstname,lastname,username\n{huge},Anić,ana.anic\n"),
            Throws.TypeOf<ImportFileRejectedException>().With.Message.Contains("predugacko"));
    }



    /// <summary>Мери потрошене бајтове без ослањања на Length или Seek.</summary>
    private sealed class CountingInput(int totalBytes) : Stream
    {
        public int TotalBytes { get; } = totalBytes;
        public int BytesRead { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => ReadInto(buffer.AsSpan(offset, count));
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(ReadInto(buffer.Span));
        }
        private int ReadInto(Span<byte> buffer)
        {
            var count = Math.Min(buffer.Length, TotalBytes - BytesRead);
            buffer[..count].Fill((byte)'a');
            BytesRead += count;
            return count;
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}
