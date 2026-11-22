using System.Security.Cryptography;
using Infrastructure.Identity;

namespace Infrastructure.Demo;

/// <summary>
/// US-7.7: sesija demo studenta za prikaz početne strane dok ST-002 ne uvede prijavu.
/// Token se generiše pri svakom pokretanju i ne čuva se u repozitorijumu.
/// Poziva se samo u Development okruženju (Program.cs).
/// </summary>
public static class DemoSession
{
    public static string StartForDemoStudent(
        InMemorySessionReader sessions,
        InMemoryAccountStatusReader accounts)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(accounts);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        accounts.Activate(DemoData.DemoStudentId);
        sessions.Register(token, DemoData.DemoStudentId);

        return token;
    }
}
