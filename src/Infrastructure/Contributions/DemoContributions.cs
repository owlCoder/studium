using Domain.Models;

namespace Infrastructure.Contributions;

public static class DemoContributions
{
    public static InMemoryContributionReader Create()
    {
        var reader = new InMemoryContributionReader();

        // 1) Podnet, sa vezom kao dokazom
        reader.Add(new Contribution(1, 101, 10, 1,
            "Uradjena pretraga predmeta.",
            new[] { new Evidence { Id = 1, Link = "https://github.com/owlCoder/studium/pull/1" } }));

        // 2) Tudji doprinos (drugi autor)
        reader.Add(new Contribution(2, 102, 20, 1,
            "Uradjen unos zaduzenja.",
            new[] { new Evidence { Id = 2, FilePath = "dokazi/unos-zaduzenja.pdf" } }));

        // 3) Vracen na dopunu pa dopunjen: pokazuje istoriju revizija
        var c3 = new Contribution(3, 103, 10, 2,
            "Radio na prikazu.",
            Array.Empty<Evidence>());
        c3.ReturnForSupplement("Dopuni opis konkretnim zadacima.", out _);
        c3.Supplement(10, "Radio na prikazu liste i detalja doprinosa.",
            new[] { new Evidence { Id = 3, Link = "https://example.com/dokaz" } }, out _);
        reader.Add(c3);

        // 4) Vracen na dopunu, jos nije dopunjen
        var c4 = new Contribution(4, 104, 10, 2, "Testovi.", Array.Empty<Evidence>());
        c4.ReturnForSupplement("Priloži dokaz.", out _);
        reader.Add(c4);

        return reader;
    }
}
