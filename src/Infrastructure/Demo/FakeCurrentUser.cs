using Application.Identity;

namespace Infrastructure.Demo;

/// <summary>
/// Privremena zamjena dok tim za ST-001/ST-002 ne isporuči pravu prijavu.
/// Zamjenjuje se za KT2.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid id, string username, UserRole role)
    {
        Id = id;
        Username = username;
        Role = role;
    }

    public Guid Id { get; }
    public string Username { get; }
    public UserRole Role { get; }

    public static FakeCurrentUser Administrator() =>
        new(Guid.NewGuid(), "admin", UserRole.Administrator);

    public static FakeCurrentUser Student() =>
        new(Guid.NewGuid(), "student", UserRole.Student);
}
