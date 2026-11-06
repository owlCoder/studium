namespace Application.Submissions;

public class SubmissionAccessPolicy
{
    private readonly IStaffAccessChecker _staffAccessChecker;

    public SubmissionAccessPolicy(IStaffAccessChecker staffAccessChecker){
        _staffAccessChecker = staffAccessChecker ?? throw new ArgumentNullException(nameof(staffAccessChecker));
    }
    public void EnsureAccess(Guid currentUserId, bool isStudent, Guid submissionOwnerId, Guid staffId, Guid courseId){
        //Dozvoljeno studentu koji je vlasnik rada
        if (isStudent && currentUserId == submissionOwnerId){
            return;
        }
        //Dozvoljeno nastavniku koji je angazovan na predmetu
        if (!isStudent && _staffAccessChecker.IsStaffAssignedToCourse(staffId, courseId)){
            return;
        }
        
        throw new UnauthorizedAccessException("Pristup odbijen: Nemate pravo da vidite ovu predaju.");
    }
}