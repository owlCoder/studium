using System;

namespace Studium.Domain.Reviews
{
    public class PregledVerzije
    {
        public Guid Id { get; private set; }
        
        
        public Guid SubmissionId { get; private set; } 
        
        public string Komentar { get; private set; }
        public string Ishod { get; private set; }

        public PregledVerzije(Guid id, Guid submissionId, string komentar, string ishod)
        {
            if (submissionId == Guid.Empty)
                throw new ArgumentException("Pregled mora biti vezan za ID konkretne verzije predaje.");

            Id = id;
            SubmissionId = submissionId;
            Komentar = komentar;
            Ishod = ishod;
        }
    }
}