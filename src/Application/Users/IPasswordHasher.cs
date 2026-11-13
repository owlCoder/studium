namespace Application.Users;

/// <summary>
/// Port za heširanje lozinki. Aktivacija, prijava i promena lozinke koriste samo ovaj interfejs.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Vraća heš sa ugrađenom solju i parametrima; isti ulaz svaki put daje drugačiji rezultat.</summary>
    string Hash(string password);

    /// <summary>Vraća false za pogrešnu lozinku i za neispravan ili prazan heš; ne baca izuzetak.</summary>
    bool Verify(string password, string storedHash);
}
