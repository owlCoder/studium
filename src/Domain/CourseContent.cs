using System;

namespace Domain.Entities;

public class CourseContent
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Guid? TargetGroupId { get; set; }
}
