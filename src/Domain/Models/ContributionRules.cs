using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Models;

public static class ContributionRules
{
    public static bool CanCreateContribution(int taskId, IEnumerable<int> existingTaskIds, out string error)
    {
        if (taskId <= 0)
        {
            error = "Doprinos mora biti vezan za zaduzenje.";
            return false;
        }
        if (existingTaskIds == null || !existingTaskIds.Contains(taskId))
        {
            error = $"Zaduzenje sa ID {taskId} ne postoji.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
