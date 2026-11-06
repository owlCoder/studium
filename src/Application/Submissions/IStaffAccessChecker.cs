namespace Application.Submissions;

public interface IStaffAccessChecker
{
    bool IsStaffAssignedToCourse(Guid staffId, Guid courseId);
}