using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Teams;

public sealed class TeamTask
{
    public Guid Id { get; }
    public Guid TeamID { get; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TeamTaskStatus Status { get; set; }

    public Guid PhaseId {  get; set; }
    public Guid ResponibleMemberId { get; set; }
    public  Guid LastChangedBy {  get; set; }
    public DateTime LastChangedAt {  get; set; }

    public TeamTask(Guid id, Guid teamId, string name,string desc, TeamTaskStatus status,Guid phase,Guid respMemb,Guid lcby,DateTime lcat){

        if(id == Guid.Empty)
        {
            throw new ArgumentException("Id mora biti unet", nameof(id));
        }
        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ime mora biti uneto", nameof(name));
        }

        Id= id;
        TeamID= teamId;
        Name= name.Trim();
        Description= desc?.Trim() ?? string.Empty;
        Status= TeamTaskStatus.Planned;
        PhaseId= phase;
        ResponibleMemberId= respMemb;
        LastChangedBy= lcby;
        LastChangedAt= lcat;
    }

    public void ChangedStatus(TeamTaskStatus status, Guid lcby, DateTime lcat)
    {
        Status= status;
        LastChangedBy= lcby;
        LastChangedAt = lcat;
    }

}


