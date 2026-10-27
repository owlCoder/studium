namespace Studium.Application.Courses;

public sealed class CourseListItem
{
    public string Code { get; }
    public string Name { get; }

    public CourseListItem(string code, string name)
    {
        Code = code;
        Name = name;
    }
}
