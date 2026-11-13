using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Models;

public static class TeamTaskRules
{
    public static bool CanCreateTask(Guid teamId, IEnumerable<Guid> existingTeamIds, out string error)
    {
        if (teamId == Guid.Empty)
        {
            error = "Zaduženje mora biti vezano za tim.";
            return false;
        }

        if (existingTeamIds == null || !existingTeamIds.Contains(teamId))
        {
            error = $"Tim sa ID {teamId} ne postoji.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
