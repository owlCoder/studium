namespace Domain.Courses;

public static class SubmissionAccessPolicy
{
    /// <summary>
    /// Provjerava ima li student pravo pristupa predanom radu.
    /// Pravilo ST-020: Autor rada UVIJEK zadržava pristup svom radu.
    /// </summary>
    public static bool CanAccessSubmission(string requestingStudentId, string submissionAuthorId, bool isGroupInTargetAudience)
    {
        // Ako je korisnik autor rada, ima pristup bez obzira na grupu
        if (requestingStudentId == submissionAuthorId)
        {
            return true;
        }

        // Ako nije autor, vidi rad samo ako je njegova grupa u ciljnoj publici
        return isGroupInTargetAudience;
    }
}
