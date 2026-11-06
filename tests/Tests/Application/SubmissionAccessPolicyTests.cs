using NUnit.Framework;
using Moq;
using Application.Submissions;

namespace Application.UnitTests.Submissions;

[TestFixture]
public class SubmissionAccessPolicyTests
{
    [Test]
    public void EnsureAccess_KadaJeStudentVlasnik_DozvoljavaPristup()
    {
        // Arrange
        var staffCheckerMock = new Mock<IStaffAccessChecker>();
        var policy = new SubmissionAccessPolicy(staffCheckerMock.Object);
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        // Act & Assert
        Assert.DoesNotThrow(() => 
            policy.EnsureAccess(studentId, isStudent: true, submissionOwnerId: studentId, Guid.NewGuid(), courseId));
    }

    [Test]
    public void EnsureAccess_KadaStudentNijeVlasnik_BacaUnauthorized()
    {
        // Arrange
        var staffCheckerMock = new Mock<IStaffAccessChecker>();
        var policy = new SubmissionAccessPolicy(staffCheckerMock.Object);
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => 
            policy.EnsureAccess(studentId, isStudent: true, submissionOwnerId: otherStudentId, Guid.NewGuid(), courseId));
    }

    [Test]
    public void EnsureAccess_KadaJeNastavnikAngazovan_DozvoljavaPristup()
    {
        // Arrange
        var staffCheckerMock = new Mock<IStaffAccessChecker>();
        var staffId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        staffCheckerMock.Setup(x => x.IsStaffAssignedToCourse(staffId, courseId)).Returns(true);
        var policy = new SubmissionAccessPolicy(staffCheckerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => 
            policy.EnsureAccess(Guid.NewGuid(), isStudent: false, Guid.NewGuid(), staffId, courseId));
    }

    [Test]
    public void EnsureAccess_KadaNastavnikNijeAngazovan_BacaUnauthorized()
    {
        // Arrange
        var staffCheckerMock = new Mock<IStaffAccessChecker>();
        var staffId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        staffCheckerMock.Setup(x => x.IsStaffAssignedToCourse(staffId, courseId)).Returns(false);
        var policy = new SubmissionAccessPolicy(staffCheckerMock.Object);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => 
            policy.EnsureAccess(Guid.NewGuid(), isStudent: false, Guid.NewGuid(), staffId, courseId));
    }
}