namespace Application.Submissions;

using Domain;

/// <summary>
/// ST-013: kategorije pregleda pristiglih radova za izabrani zadatak.
/// </summary>
public enum SubmissionReviewCategory
{
    /// <summary>Student je član, ali nema važeću predaju.</summary>
    WithoutSubmission,

    /// <summary>Važeća predaja postoji i čeka pregled.</summary>
    Unreviewed,

    /// <summary>Važeća predaja postoji i pregledana je.</summary>
    Reviewed
}

/// <summary>
/// Jedan red pregleda: student (iz članstva) i njegova važeća predaja, ako postoji.
/// </summary>
public sealed record SubmissionReviewEntry(
    MemberContract Student,
    SubmissionContract? ValidSubmission,
    SubmissionReviewCategory Category
);

/// <summary>
/// Tri liste pregleda za jedan zadatak. Svaki student iz članstva nalazi se u tačno jednoj listi.
/// </summary>
public sealed record TaskSubmissionReviewLists(
    Guid TaskId,
    IReadOnlyList<SubmissionReviewEntry> WithoutSubmission,
    IReadOnlyList<SubmissionReviewEntry> Unreviewed,
    IReadOnlyList<SubmissionReviewEntry> Reviewed
);

/// <summary>
/// ST-013: brojači stanja predaja. Mogu nastati isključivo iz listi pregleda,
/// pa uvek koriste isti kriterijum važeće predaje kao i liste.
/// </summary>
public sealed class SubmissionReviewCounters
{
    private SubmissionReviewCounters(int withoutSubmission, int unreviewed, int reviewed)
    {
        WithoutSubmission = withoutSubmission;
        Unreviewed = unreviewed;
        Reviewed = reviewed;
    }

    public int WithoutSubmission { get; }
    public int Unreviewed { get; }
    public int Reviewed { get; }

    public static SubmissionReviewCounters FromLists(TaskSubmissionReviewLists lists)
    {
        ArgumentNullException.ThrowIfNull(lists);

        return new SubmissionReviewCounters(
            lists.WithoutSubmission.Count,
            lists.Unreviewed.Count,
            lists.Reviewed.Count);
    }
}

/// <summary>
/// ST-013: povezuje članstvo i predaje u liste pregleda, bez praznih fiktivnih radova.
/// </summary>
public static class TaskSubmissionReviewClassifier
{
    /// <param name="taskId">Izabrani zadatak.</param>
    /// <param name="students">Studenti članovi izvođenja kome zadatak pripada.</param>
    /// <param name="submissions">Predaje; predaje drugih zadataka i ne-članova se zanemaruju.</param>
    public static TaskSubmissionReviewLists Classify(
        Guid taskId,
        IEnumerable<MemberContract> students,
        IEnumerable<SubmissionContract> submissions)
    {
        ArgumentNullException.ThrowIfNull(students);
        ArgumentNullException.ThrowIfNull(submissions);

        var validSubmissionByStudent = submissions
            .Where(s => s.TaskId == taskId)
            .GroupBy(s => s.MemberId)
            .ToDictionary(
                g => g.Key,
                // Važeća predaja = poslednja evidentirana (ST-012: zamena postaje važeća).
                g => g.OrderByDescending(s => s.SubmittedAt).First());

        var withoutSubmission = new List<SubmissionReviewEntry>();
        var unreviewed = new List<SubmissionReviewEntry>();
        var reviewed = new List<SubmissionReviewEntry>();

        foreach (var student in students.DistinctBy(m => m.MemberId))
        {
            validSubmissionByStudent.TryGetValue(student.MemberId, out var submission);
            var category = Categorize(submission);
            var entry = new SubmissionReviewEntry(student, submission, category);

            switch (category)
            {
                case SubmissionReviewCategory.WithoutSubmission:
                    withoutSubmission.Add(entry);
                    break;
                case SubmissionReviewCategory.Unreviewed:
                    unreviewed.Add(entry);
                    break;
                default:
                    reviewed.Add(entry);
                    break;
            }
        }

        return new TaskSubmissionReviewLists(taskId, withoutSubmission, unreviewed, reviewed);
    }

    private static SubmissionReviewCategory Categorize(SubmissionContract? submission) =>
        submission switch
        {
            null => SubmissionReviewCategory.WithoutSubmission,
            { Status: SubmissionStatus.Pending } => SubmissionReviewCategory.Unreviewed,
            _ => SubmissionReviewCategory.Reviewed
        };
}
