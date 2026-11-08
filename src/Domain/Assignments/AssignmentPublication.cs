namespace Domain.Assignments;

public static class AssignmentPublication
{
    public static IReadOnlyList<string> GetMissingPublicationFields(
        string? title, string? description, string? instructions, DateTimeOffset? deadline)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
            missing.Add("naziv");
        if (string.IsNullOrWhiteSpace(description))
            missing.Add("opis");
        if (string.IsNullOrWhiteSpace(instructions))
            missing.Add("uputstvo");
        if (deadline is null)
            missing.Add("rok");

        return missing;
    }

    public static string? GetPublicationError(
        bool isDraft, string? title, string? description, string? instructions, DateTimeOffset? deadline)
    {
        if (!isDraft)
            return "Samo zadatak u nacrtu može da se objavi.";

        var missing = GetMissingPublicationFields(title, description, instructions, deadline);
        if (missing.Count > 0)
            return $"Nedostaju obavezni podaci: {string.Join(", ", missing)}.";

        return null;
    }
}
