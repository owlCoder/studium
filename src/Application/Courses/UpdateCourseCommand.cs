using System;

namespace Application.Courses;

public sealed record UpdateCourseCommand(Guid CourseId, string Code, string Name,string Description);
