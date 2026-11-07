namespace Tests.Application;

using global::Application.Submissions;
using global::Domain;
using NUnit.Framework;

[TestFixture]
public class SubmissionReviewCountersTests
{
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly DateTime BaseTime = new(2026, 11, 10, 20, 0, 0, DateTimeKind.Utc);

    private static MemberContract Student(int i) =>
        new(Guid.NewGuid(), $"Student {i}", "Student");

    private static SubmissionContract Submission(
        MemberContract student,
        SubmissionStatus status,
        Guid? taskId = null,
        DateTime? submittedAt = null) =>
        new(Guid.NewGuid(), taskId ?? TaskId, student.MemberId,
            "https://example.test/rad", submittedAt ?? BaseTime, status);

    private static void AssertCountersMatchLists(TaskSubmissionReviewLists lists)
    {
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.WithoutSubmission, Is.EqualTo(lists.WithoutSubmission.Count));
            Assert.That(counters.Unreviewed, Is.EqualTo(lists.Unreviewed.Count));
            Assert.That(counters.Reviewed, Is.EqualTo(lists.Reviewed.Count));
        });
    }

    // --- Pozitivni: primer iz specifikacije ---

    [Test]
    public void PrimerIzSpecifikacije_DesetStudenataSedamPredajaCetiriPregledane()
    {
        var students = Enumerable.Range(1, 10).Select(Student).ToList();
        var submissions = new List<SubmissionContract>
        {
            Submission(students[0], SubmissionStatus.Accepted),
            Submission(students[1], SubmissionStatus.Accepted),
            Submission(students[2], SubmissionStatus.NeedsRevision),
            Submission(students[3], SubmissionStatus.Rejected),
            Submission(students[4], SubmissionStatus.Pending),
            Submission(students[5], SubmissionStatus.Pending),
            Submission(students[6], SubmissionStatus.Pending),
        };

        var lists = TaskSubmissionReviewClassifier.Classify(TaskId, students, submissions);
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.WithoutSubmission, Is.EqualTo(3));
            Assert.That(counters.Unreviewed, Is.EqualTo(3));
            Assert.That(counters.Reviewed, Is.EqualTo(4));
            // "Bez predaje" i "Nepregledano" nisu ista lista ni isti podatak.
            Assert.That(lists.WithoutSubmission.Select(e => e.Student.MemberId),
                Is.EquivalentTo(students.Skip(7).Select(s => s.MemberId)));
            Assert.That(lists.Unreviewed.Select(e => e.Student.MemberId),
                Is.EquivalentTo(students.Skip(4).Take(3).Select(s => s.MemberId)));
        });
        AssertCountersMatchLists(lists);
    }

    // --- Invarijanta: brojač je uvek jednak dužini odgovarajuće liste ---

    [TestCase(0, 0, 0)]
    [TestCase(5, 0, 0)]
    [TestCase(0, 5, 0)]
    [TestCase(0, 0, 5)]
    [TestCase(2, 3, 4)]
    [TestCase(1, 1, 1)]
    public void BrojacJeUvekJednakDuziniListe(int withoutSubmission, int unreviewed, int reviewed)
    {
        var students = new List<MemberContract>();
        var submissions = new List<SubmissionContract>();

        for (var i = 0; i < withoutSubmission; i++)
            students.Add(Student(i));
        for (var i = 0; i < unreviewed; i++)
        {
            var s = Student(i);
            students.Add(s);
            submissions.Add(Submission(s, SubmissionStatus.Pending));
        }
        for (var i = 0; i < reviewed; i++)
        {
            var s = Student(i);
            students.Add(s);
            submissions.Add(Submission(s, SubmissionStatus.Accepted));
        }

        var lists = TaskSubmissionReviewClassifier.Classify(TaskId, students, submissions);
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.WithoutSubmission, Is.EqualTo(withoutSubmission));
            Assert.That(counters.Unreviewed, Is.EqualTo(unreviewed));
            Assert.That(counters.Reviewed, Is.EqualTo(reviewed));
        });
        AssertCountersMatchLists(lists);
    }

    [Test]
    public void SviStudentiSuUTacnoJednojListi()
    {
        var students = Enumerable.Range(1, 6).Select(Student).ToList();
        var submissions = new[]
        {
            Submission(students[0], SubmissionStatus.Pending),
            Submission(students[1], SubmissionStatus.NeedsRevision),
        };

        var lists = TaskSubmissionReviewClassifier.Classify(TaskId, students, submissions);
        var all = lists.WithoutSubmission.Concat(lists.Unreviewed).Concat(lists.Reviewed)
            .Select(e => e.Student.MemberId).ToList();

        Assert.That(all, Is.EquivalentTo(students.Select(s => s.MemberId)));
        AssertCountersMatchLists(lists);
    }

    // --- Negativni: nema fiktivnih radova ---

    [Test]
    public void PredajaNeClana_SeNeBroji()
    {
        var member = Student(1);
        var outsider = Student(2);
        var submissions = new[]
        {
            Submission(member, SubmissionStatus.Pending),
            Submission(outsider, SubmissionStatus.Pending),
        };

        var lists = TaskSubmissionReviewClassifier.Classify(TaskId, new[] { member }, submissions);
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.Unreviewed, Is.EqualTo(1));
            Assert.That(lists.Unreviewed.Single().Student.MemberId, Is.EqualTo(member.MemberId));
        });
        AssertCountersMatchLists(lists);
    }

    [Test]
    public void PredajaZaDrugiZadatak_SeNeBroji()
    {
        var student = Student(1);
        var submissions = new[] { Submission(student, SubmissionStatus.Accepted, taskId: Guid.NewGuid()) };

        var lists = TaskSubmissionReviewClassifier.Classify(TaskId, new[] { student }, submissions);
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.WithoutSubmission, Is.EqualTo(1));
            Assert.That(counters.Reviewed, Is.EqualTo(0));
        });
        AssertCountersMatchLists(lists);
    }

    [Test]
    public void NullUlazi_BacajuArgumentNullException()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                TaskSubmissionReviewClassifier.Classify(TaskId, null!, Array.Empty<SubmissionContract>()));
            Assert.Throws<ArgumentNullException>(() =>
                TaskSubmissionReviewClassifier.Classify(TaskId, Array.Empty<MemberContract>(), null!));
            Assert.Throws<ArgumentNullException>(() => SubmissionReviewCounters.FromLists(null!));
        });
    }

    // --- Granični slučajevi ---

    [Test]
    public void ViseZapisaIstogStudenta_BrojiSeJednaVazecaPredaja()
    {
        var student = Student(1);
        var submissions = new[]
        {
            Submission(student, SubmissionStatus.Accepted, submittedAt: BaseTime),
            Submission(student, SubmissionStatus.Pending, submittedAt: BaseTime.AddHours(1)),
        };

        var lists = TaskSubmissionReviewClassifier.Classify(TaskId, new[] { student }, submissions);
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.Unreviewed, Is.EqualTo(1));
            Assert.That(counters.Reviewed, Is.EqualTo(0));
            Assert.That(lists.Unreviewed.Single().ValidSubmission!.SubmittedAt,
                Is.EqualTo(BaseTime.AddHours(1)));
        });
        AssertCountersMatchLists(lists);
    }

    [Test]
    public void DupliranStudentUClanstvu_BrojiSeJednom()
    {
        var student = Student(1);

        var lists = TaskSubmissionReviewClassifier.Classify(
            TaskId, new[] { student, student }, Array.Empty<SubmissionContract>());

        Assert.That(SubmissionReviewCounters.FromLists(lists).WithoutSubmission, Is.EqualTo(1));
        AssertCountersMatchLists(lists);
    }

    [Test]
    public void BezClanova_SviBrojaciSuNula_IakoPostojePredaje()
    {
        var orphan = Submission(Student(1), SubmissionStatus.Pending);

        var lists = TaskSubmissionReviewClassifier.Classify(
            TaskId, Array.Empty<MemberContract>(), new[] { orphan });
        var counters = SubmissionReviewCounters.FromLists(lists);

        Assert.Multiple(() =>
        {
            Assert.That(counters.WithoutSubmission, Is.EqualTo(0));
            Assert.That(counters.Unreviewed, Is.EqualTo(0));
            Assert.That(counters.Reviewed, Is.EqualTo(0));
        });
    }

    [TestCase(SubmissionStatus.Pending, SubmissionReviewCategory.Unreviewed)]
    [TestCase(SubmissionStatus.Accepted, SubmissionReviewCategory.Reviewed)]
    [TestCase(SubmissionStatus.NeedsRevision, SubmissionReviewCategory.Reviewed)]
    [TestCase(SubmissionStatus.Rejected, SubmissionReviewCategory.Reviewed)]
    public void StatusPredaje_OdredjujeKategoriju(SubmissionStatus status, SubmissionReviewCategory expected)
    {
        var student = Student(1);

        var lists = TaskSubmissionReviewClassifier.Classify(
            TaskId, new[] { student }, new[] { Submission(student, status) });
        var entry = lists.WithoutSubmission.Concat(lists.Unreviewed).Concat(lists.Reviewed).Single();

        Assert.That(entry.Category, Is.EqualTo(expected));
        AssertCountersMatchLists(lists);
    }
}
