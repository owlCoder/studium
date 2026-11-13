using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities;

public class ContentTargetAudience
{
    public Guid Id { get; set; }
    public Guid ContentId { get; set; }
    public bool IsForWholeCourseExecution { get; set; }
    public List<Guid> TargetGroupIds { get; set; } = new();

    public ContentTargetAudience() { }

    public ContentTargetAudience(Guid contentId, bool isForWholeCourseExecution, List<Guid>? targetGroupIds = null)
    {
        Id = Guid.NewGuid();
        ContentId = contentId;
        IsForWholeCourseExecution = isForWholeCourseExecution;

        // Poslovno pravilo: Prazan spisak grupa nije zamena za izbor celog izvođenja
        if (!isForWholeCourseExecution)
        {
            if (targetGroupIds == null || !targetGroupIds.Any())
            {
                throw new InvalidOperationException("Morate izabrati bar jednu nastavnu grupu ako sadržaj nije namenjen celom izvođenju.");
            }
            TargetGroupIds = targetGroupIds;
        }
    }
}