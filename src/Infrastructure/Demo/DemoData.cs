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
    private static readonly Course[] DemoCourses =
     {
        new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ERS", "Elementi razvoja softvera"),
        new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "OIB", "Osnove informacione bezbednosti"),
        new Course(Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "BP", "Baze podataka", isActive: false)
    };

    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(DemoCourses);
    }

    public static ICourseStatusRepository CreateCourseStatusRepository()
    {
        return new InMemoryCourseStatusRepository(DemoCourses);
    }

    /// <summary>
    /// Sintetički demo korisnici za ručno isprobavanje prijave i ovlašćenja (ST-002, ST-003).
    /// Lozinka je izmišljena i važi samo za demonstraciju; ne koristiti je za stvarne naloge.
    /// </summary>
    public static IReadOnlyList<User> CreateUsers(IPasswordHasher passwordHasher)
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
                UserRole.Assistant, passwordHash: null, isActive: true),

            // NOVO: aktivan administrator, za ST-003 (aktivacija predmeta)
            new User(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "admin",
                UserRole.Administrator, passwordHasher.Hash(demoPassword), isActive: true)
        };
    }

    public static IUserLookup CreateUserLookup(IPasswordHasher passwordHasher)
    {
        return new InMemoryUserLookup(CreateUsers(passwordHasher));
    }
}
