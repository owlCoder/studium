namespace Application.Access;

public enum AccessDecision
{
    Allowed,
    Unauthenticated, // nema identiteta, nalog ne postoji, deaktiviran ili neaktiviran
    Forbidden        // nalog je u redu, ali uloga nije dozvoljena
}
