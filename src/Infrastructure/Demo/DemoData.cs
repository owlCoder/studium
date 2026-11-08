using Application.Courses;
using Application.Auth;
using Application.Users;
using Domain.Courses;
using Domain.Users;
using Infrastructure.Courses;
using Infrastructure.Auth;

namespace Infrastructure.Demo;

public static class DemoData
{
    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(new[]
        {
            new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "ERS", "Elementi razvoja softvera"),
            new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "OIB", "Osnove informacione bezbednosti")
        });
    }

    /// <summary>
    /// Sintetički demo korisnici (ST-002). Izdvojeni da bi prijava i promena lozinke
    /// mogle da dele isti skup naloga. Lozinka je izmišljena i važi samo za demonstraciju;
    /// ne koristiti je za stvarne naloge.
    /// </summary>
    public static IReadOnlyList<User> CreateDemoUsers(IPasswordHasher passwordHasher)
    {
        const string demoPassword = "Demo-Lozinka-1";

        return new[]
        {
            // Aktivan student: ana.petrovic
            new User(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Ana.Petrovic",
                UserRole.Student, passwordHasher.Hash(demoPassword), isActive: true),

            // Deaktiviran nalog: stari.nalog
            new User(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "stari.nalog",
                UserRole.Teacher, passwordHasher.Hash(demoPassword), isActive: false),

            // Aktivacija nije završena (nema lozinku): novi.nalog
            new User(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "novi.nalog",
                UserRole.Assistant, passwordHash: null, isActive: true)
        };
    }

    /// <summary>
    /// Demo korisnici za ručno isprobavanje prijave (ST-002).
    /// </summary>
    public static IUserLookup CreateUserLookup(IPasswordHasher passwordHasher)
    {
        return new InMemoryUserLookup(CreateDemoUsers(passwordHasher));
    }
}
