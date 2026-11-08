using System;

namespace Domain.Entities;

public class CourseContent
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid? TargetGroupId { get; set; }

    public CourseContent(Guid id, Guid courseId, string title)
    {
        Id = id;
        CourseId = courseId;
        Title = title ?? string.Empty;
    }
    public CourseContent() { }
}
