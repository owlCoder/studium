using System;
using System.Collections.Generic;

namespace Application.ContentAudience;

public record SetContentTargetAudienceCommand(
    Guid ContentId,
    Guid InstructorId,
    bool IsForWholeCourseExecution,
    List<Guid> TargetGroupIds
);