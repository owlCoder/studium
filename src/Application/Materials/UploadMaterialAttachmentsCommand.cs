namespace Application.Materials;

/// <summary>
/// Zahtev za otpremanje jedne ili više datoteka u materijal (ST-009, US-04).
/// </summary>
public sealed record UploadMaterialAttachmentsCommand(
    Guid RequestingUserId,
    Guid MaterialId,
    IReadOnlyList<UploadedFile> Files);

/// <summary>
/// Jedna datoteka iz zahteva za otpremanje. Sadržaj se otvara tek kada dođe red
/// na tu datoteku, pa sloj <c>Application</c> ne zavisi od HTTP tipova.
/// <paramref name="Length"/> je veličina koju prijavljuje ulaz (npr. multipart deo);
/// stvarna veličina se ponovo meri pri čuvanju (US-05).
/// </summary>
public sealed record UploadedFile(
    string FileName,
    string ContentType,
    long Length,
    Func<Stream> OpenReadStream);

/// <summary>
/// Sačuvan prilog, kako se vraća pozivaocu nakon otpremanja.
/// </summary>
public sealed record UploadedAttachment(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes);
