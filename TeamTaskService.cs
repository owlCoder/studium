
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models;

public class TeamTaskService
{
    private readonly string _filePath = "Data/tasks.json";

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public async Task<List<TeamTask>> GetAllTasksAsync()
    {
        if (!File.Exists(_filePath))
            return new List<TeamTask>();

        string json = await File.ReadAllTextAsync(_filePath);

        return JsonSerializer.Deserialize<List<TeamTask>>(
            json, _options
        ) ?? new List<TeamTask>();
    }

    public async Task<List<TeamTask>> GetMyTasksAsync(
        Guid studentId)
    {
        var tasks = await GetAllTasksAsync();

        return tasks
            .Where(t => t.ResponsibleMemberId == studentId)
            .OrderBy(t => t.Name)
            .ToList();
    }
}
