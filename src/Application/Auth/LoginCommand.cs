namespace Application.Auth;

/// <summary>Ulaz slučaja upotrebe "Prijava". Vrednosti su sirove, validira ih handler.</summary>
public sealed record LoginCommand(string? Username, string? Password);
