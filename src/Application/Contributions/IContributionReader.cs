using Domain.Models;

namespace Application.Contributions;

public interface IContributionReader
{
    Contribution? GetById(int id);
    IReadOnlyList<Contribution> GetByAuthor(int authorId);
    IReadOnlyList<Contribution> GetAll();
}
