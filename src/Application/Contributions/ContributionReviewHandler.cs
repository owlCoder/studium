using Domain.Models;

namespace Application.Contributions;

public record ReviewResult(bool Success, string Message)
{
    public static ReviewResult Ok(string message) => new(true, message);
    public static ReviewResult Fail(string message) => new(false, message);
}

public class ContributionReviewHandler
{
    private const string NotTeacher = "Samo nastavno osoblje može da pregleda doprinose.";
    private const string NotFound = "Doprinos ne postoji.";

    private readonly IContributionRepository _repository;

    public ContributionReviewHandler(IContributionRepository repository) => _repository = repository;

    public ReviewResult Confirm(Viewer viewer, int contributionId, string reviewerNote)
    {
        var contribution = FindForReview(viewer, contributionId, out var failure);
        if (contribution is null)
            return failure;

        if (!contribution.Confirm(viewer.UserId, reviewerNote, out var error))
            return ReviewResult.Fail(error);

        _repository.Save(contribution);
        return ReviewResult.Ok("Doprinos je potvrđen.");
    }

    public ReviewResult ReturnForSupplement(Viewer viewer, int contributionId, string reviewerNote)
    {
        var contribution = FindForReview(viewer, contributionId, out var failure);
        if (contribution is null)
            return failure;

        if (!contribution.ReturnForSupplement(viewer.UserId, reviewerNote, out var error))
            return ReviewResult.Fail(error);

        _repository.Save(contribution);
        return ReviewResult.Ok("Doprinos je vraćen na dopunu.");
    }

    private Contribution? FindForReview(Viewer viewer, int contributionId, out ReviewResult failure)
    {
        failure = ReviewResult.Fail(NotTeacher);
        if (viewer.Role != ViewerRole.Teacher)
            return null;

        var contribution = _repository.GetById(contributionId);
        if (contribution is null)
            failure = ReviewResult.Fail(NotFound);

        return contribution;
    }
}
