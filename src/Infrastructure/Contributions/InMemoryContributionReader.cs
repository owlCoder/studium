using Application.Contributions;
using Domain.Models;

namespace Infrastructure.Contributions;

// Ako Milica vec pravi memorijski repozitorijum za Create, neka on implementira
// i IContributionReader, pa ova klasa nije potrebna.
public class InMemoryContributionReader : IContributionReader
{
    private readonly List<Contribution> _items = new();

    public InMemoryContributionReader() { }

    public InMemoryContributionReader(IEnumerable<Contribution> items) => _items.AddRange(items);

    public void Add(Contribution contribution) => _items.Add(contribution);

    public Contribution? GetById(int id) => _items.FirstOrDefault(c => c.Id == id);

    public IReadOnlyList<Contribution> GetByAuthor(int authorId) =>
        _items.Where(c => c.AuthorId == authorId).ToList();

    public IReadOnlyList<Contribution> GetAll() => _items.ToList();
}
