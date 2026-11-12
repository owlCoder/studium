using Application.Works;
using Domain.Works;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class GetStudentWorksHandlerTests
{
    private sealed class FakeWorkRepository : IStudentWorkRepository
    {
        private readonly List<StudentWork> _works;
        public FakeWorkRepository(params StudentWork[] works) => _works = works.ToList();

        public Task<IReadOnlyList<StudentWork>> GetByStudentAsync(
            Guid studentId, CancellationToken ct = default)
        {
            var result = _works.Where(w => w.StudentId == studentId).ToList();
            return Task.FromResult<IReadOnlyList<StudentWork>>(result);
        }
    }

    private static StudentWork Work(Guid studentId, string title = "Seminarski") =>
        new(Guid.NewGuid(), studentId, Guid.NewGuid(), title, DateTimeOffset.UtcNow);

    [Test]
    public async Task HandleAsync_ReturnsAllWorks_ForStudent()
    {
        var studentId = Guid.NewGuid();
        var repo = new FakeWorkRepository(Work(studentId, "Rad 1"), Work(studentId, "Rad 2"));
        var handler = new GetStudentWorksHandler(repo);

        var result = await handler.HandleAsync(studentId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task HandleAsync_ReturnsEmpty_ForStudentWithoutWorks()
    {
        var repo = new FakeWorkRepository();
        var handler = new GetStudentWorksHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void HandleAsync_RejectsEmptyStudentId()
    {
        var handler = new GetStudentWorksHandler(new FakeWorkRepository());

        Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(Guid.Empty));
    }

    [Test]
    public async Task HandleAsync_ReturnsGradedWorks()
    {
        var studentId = Guid.NewGuid();
        var work = Work(studentId, "Ocenjeni rad");
        work.SetGrade(9);
        var repo = new FakeWorkRepository(work);
        var handler = new GetStudentWorksHandler(repo);

        var result = await handler.HandleAsync(studentId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Grade, Is.EqualTo(9));
        Assert.That(result[0].Status, Is.EqualTo(WorkStatus.Graded));
    }

    // ⬇️ KLJUČNO — bez Enrollment zavisnosti
    [Test]
    public async Task HandleAsync_ReturnsWorks_RegardlessOfEnrollmentStatus()
    {
        // Ključno : radovi ostaju vidljivi i posle prekida članstva.
        // Handler ne filtrira po statusu članstva — svi radovi se vraćaju.
        var studentId = Guid.NewGuid();
        var work1 = Work(studentId, "Stari rad 1");
        var work2 = Work(studentId, "Stari rad 2");
        var repo = new FakeWorkRepository(work1, work2);
        var handler = new GetStudentWorksHandler(repo);

        var result = await handler.HandleAsync(studentId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(w => w.Title), Does.Contain("Stari rad 1"));
    }
}
