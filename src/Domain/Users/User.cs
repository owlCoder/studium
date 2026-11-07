using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Users;

/// <summary>
/// Minimalan model korisničkog naloga potreban za prijavljivanje (ST-002).
///
/// Odgovornost (SRP): čuva podatke naloga i odgovara na pitanje "da li nalog sme da se koristi".
/// Ne zna ništa o heširanju, bazi ni HTTP-u.
///
/// NAPOMENA: model je zajednički sa ST-001 (ime, prezime, e-pošta...) i dopunjava se dogovorom
/// sa Jovanom. Ovde su samo polja koja prijava stvarno koristi.
/// </summary>
public sealed class User
{
    public Guid Id { get; }

    /// <summary>Korisničko ime onako kako je uneto (za prikaz).</summary>
    public string Username { get; }

    public UserRole Role { get; }

    /// <summary>
    /// Heš lozinke. <c>null</c> znači da aktivacija još nije završena, jer se prva lozinka
    /// postavlja tek aktivacijom (ST-001/ST-002). Čista lozinka se nikada ne čuva (zahtev 4.2).
    /// </summary>
    public string? PasswordHash { get; }

    /// <summary>Stanje naloga: Aktivan (true) ili Deaktiviran (false).</summary>
    public bool IsActive { get; }

    /// <summary>Aktivacija je završena kada postoji prva lozinka.</summary>
    public bool IsActivated => PasswordHash is not null;

    public User(Guid id, string username, UserRole role, string? passwordHash, bool isActive)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Korisničko ime je obavezno.", nameof(username));
        }

        if (passwordHash is not null && string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Heš lozinke ne sme biti prazan.", nameof(passwordHash));
        }

        Id = id;
        Username = username.Trim();
        Role = role;
        PasswordHash = passwordHash;
        IsActive = isActive;
    }

    /// <summary>
    /// Jedino mesto gde se definiše poređenje korisničkih imena: bez razlike u veličini slova
    /// (ST-001: "Ana.Petrovic" je isto što i "ana.petrovic"). Isti oblik koristi i jedinstveni
    /// indeks u bazi (lower(username)).
    /// </summary>
    public static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    /// <summary>Nalog sme da koristi zaštićene funkcije samo ako je aktiviran i aktivan.</summary>
    public bool CanUseProtectedFunctions()
    {
        return IsActivated && IsActive;
    }
}

