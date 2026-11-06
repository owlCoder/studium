namespace Application.Courses;

using Domain;

public class SelectContentGroupsService
{
    public ContentGroupContract AssignGroupsToContent(Guid contentId, List<Guid> selectedGroupIds)
    {
        if (selectedGroupIds == null || !selectedGroupIds.Any())
        {
            throw new ArgumentException("Mora biti izabrana najmanje jedna grupa za prikaz sadržaja.");
        }

        return new ContentGroupContract(contentId, selectedGroupIds.Distinct().ToList());
    }
}