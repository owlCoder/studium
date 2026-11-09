using Application.Courses;
using Application.Users;
using Domain.Courses;
using Domain.Users;
using Infrastructure.Courses;
using Infrastructure.Users;

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

    // Korisnici za lokalni razvoj i demonstraciju.
    // Ne koristiti ove demo naloge u produkciji.
    public static InMemoryUserRepository CreateUserRepository(
        IPasswordHasher passwordHasher)
    {
        const string demoPassword = "Demo-Lozinka-1";

        return new InMemoryUserRepository(new[]
        {
            new User(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Demo",
                "Administrator",
                "admin",
                UserRole.Administrator,
                passwordHash: passwordHasher.Hash(demoPassword)),

            new User(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "Ana",
                "Petrovic",
                "ana.petrovic",
                UserRole.Student,
                passwordHash: passwordHasher.Hash(demoPassword))
        });
    }
}
