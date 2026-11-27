using NUnit.Framework;
using Domain.TeachingSessions;
using Infrastructure.TeachingSessions;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemorySessionRepositoryTests
{
    private static TeachingSession NewSession() => new(
        Guid.NewGuid(), "Vežbe", Guid.NewGuid(), Guid.NewGuid(),
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 11, 30, 0),
        "Učionica 101");

    [Test]
    public async Task GetById_AfterAdd_ReturnsSession()
    {
        var repository = new InMemorySessionRepository();
        var session = NewSession();
        await repository.AddAsync(session);

        var found = await repository.GetByIdAsync(session.Id);

        Assert.That(found, Is.SameAs(session));
    }

    [Test]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var repository = new InMemorySessionRepository();

        var found = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task Update_ExistingSession_KeepsNewState()
    {
        var repository = new InMemorySessionRepository();
        var session = NewSession();
        await repository.AddAsync(session);

        session.MarkHeld();
        await repository.UpdateAsync(session);

        var found = await repository.GetByIdAsync(session.Id);
        Assert.That(found!.State, Is.EqualTo(SessionState.Odrzan));
    }

    [Test]
    public void Update_UnknownSession_Throws()
    {
        var repository = new InMemorySessionRepository();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(NewSession()));
    }
}
