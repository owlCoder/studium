
using Domain.Enums;
namespace Domain.Models;

public sealed class TeamTask
{
    public Guid Id { get; } 

    public Guid TeamID { get; }
    public string Name { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;
    public TeamTaskStatus Status { get; private set; }

    public Guid PhaseId { get; set; }
    public Guid ResponsibleMemberId { get; set; }
    public Guid LastChangedBy { get; set; }
    public DateTime LastChangedAt { get; set; }

    public TeamTask(Guid id, Guid teamId, string name, string desc, Guid phase, Guid respMemb, Guid lcby, DateTime lcat)
    {

    

        Id = id;
        TeamID = teamId;
        Name = name?.Trim() ?? string.Empty; 
        Description = desc?.Trim() ?? string.Empty;
        Status = TeamTaskStatus.Planned;
        PhaseId = phase;
        ResponsibleMemberId = respMemb;
        LastChangedBy = lcby;
        LastChangedAt = lcat;
    }

    public bool IsValid()
    {
        if (Id == Guid.Empty) {
            return false;
        }
        else if (string.IsNullOrWhiteSpace(Name)) {
            return false;
        }
        return true;
    }

    public void ChangedStatus(TeamTaskStatus status, Guid lcby, DateTime lcat)
    {
        Status = status;
        LastChangedBy = lcby;
        LastChangedAt = lcat;
    }
}
