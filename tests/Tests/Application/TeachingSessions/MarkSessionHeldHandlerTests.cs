using NUnit.Framework;
using Application.TeachingSessions;
using Domain.TeachingSessions;
using Infrastructure.TeachingSessions;

namespace Tests.Application.TeachingSessions;

[TestFixture]
public sealed class MarkSessionHeldHandlerTests
{
    private static TeachingSession NewSession() => new(
        Guid.NewGuid(), "Vežbe", Guid.NewGuid(), Guid.NewGuid(),
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 11, 30, 0),
        "Učionica 101");

    [Test]
    public async Task Handle_PlannedSession_BecomesHeldAndIsSaved()
    {
        var repository = new InMemorySessionRepository();
        var session = NewSession();
        await repository.AddAsync(session);

        await new MarkSessionHeldHandler(repository).HandleAsync(session.Id);

        var stored = await repository.GetByIdAsync(session.Id);
        Assert.That(stored!.State, Is.EqualTo(SessionState.Odrzan));
    }

    [Test]
    public async Task Handle_CancelledSession_IsRejectedAndStaysCancelled()
    {
        var repository = new InMemorySessionRepository();
        var session = NewSession();
        session.Cancel();
        await repository.AddAsync(session);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new MarkSessionHeldHandler(repository).HandleAsync(session.Id));

        var stored = await repository.GetByIdAsync(session.Id);
        Assert.That(stored!.State, Is.EqualTo(SessionState.Otkazan));
    }

    [Test]
    public void Handle_UnknownSession_Throws()
    {
        var repository = new InMemorySessionRepository();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await new MarkSessionHeldHandler(repository).HandleAsync(Guid.NewGuid()));

        Assert.That(ex!.Message, Is.EqualTo("Termin ne postoji."));
    }
}
