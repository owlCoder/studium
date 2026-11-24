namespace Domain.Offerings;         

public sealed class Offering        
{
    public Guid Id { get; }           
    public Guid CourseId { get; }   
    public string AcademicYear { get; } 

    public Offering(Guid id, Guid courseId, string academicYear)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator izvođenja mora biti zadat.", nameof(id));
        }
        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("Predmet izvođenja mora biti zadat.", nameof(courseId));
        }
        if (string.IsNullOrWhiteSpace(academicYear))
        {
            throw new ArgumentException("Akademska godina je obavezna.", nameof(academicYear));
        }

        Id = id;
        CourseId = courseId;
        AcademicYear = academicYear.Trim();
    }
}
