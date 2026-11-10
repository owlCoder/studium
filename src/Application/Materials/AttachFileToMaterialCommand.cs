namespace Application.Materials;

/// <summary>
/// Zahtev za vezivanje jedne datoteke za materijal (ST-009, US-06).
/// <paramref name="Content"/> je sadržaj datoteke; pozivalac ostaje njegov vlasnik.
/// </summary>
public sealed record AttachFileToMaterialCommand(
    Guid MaterialId,
    string OriginalFileName,
    string ContentType,
    Stream Content);
