using Domain.Users;

namespace Infrastructure.Users;

public static class DemoUsers
{
    public static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    public static readonly Guid StudentId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");

    public static IEnumerable<User> Seed() => new[]
    {
        new User(AdminId, "Demo", "Administrator", "admin", UserRole.Administrator),
        new User(StudentId, "Demo", "Student", "student", UserRole.Student)
    };
}
