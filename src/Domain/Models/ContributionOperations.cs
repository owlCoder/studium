using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Models;

public class ContributionOperations
{
    public bool UpdateContribution(Contribution contribution,
                                   int studentId,
                                   string newDescription,
                                   IEnumerable<Evidence> evidenceItems,
                                   out string error)

    {
        if (contribution == null)
        {
            error = "Doprinos ne postoji.:";
            return false;
        }

        if (studentId != contribution.AuthorId)
        {
            error = "Samo autor moze menjati doprinos.";
            return false;
        }

        if (contribution.Status != ContributionStatus.VracenNaDopunu)
        {
            error = "Doprinos se moze menjati samo ako je vracen na dopunu.";
            return false;
        }

        return contribution.Supplement(studentId,
                                       newDescription,
                                       evidenceItems,
                                       out error);
    }



    public bool DeleteContribution(IList<Contribution> contributions,
                                   Contribution contribution,
                                   int studentId,
                                   out string error)
    {
        if(contributions == null || contribution == null)
        {
            error = "Doprinos ili lista ne postoji.";
            return false;
        }
        if(!contributions.Contains(contribution))
        {
            error = "Doprinos nije pronadjen u listi.";
            return false;
        }
        if(studentId!=contribution.AuthorId)
        {
            error = "Samo autor moze obrisati doprinos.";
            return false;
        }
        if(contribution.Status!= ContributionStatus.VracenNaDopunu)
        {
            error = "Doprinos nije vracen na dopunu.";
            return false;
        }
        if(contributions.IsReadOnly)
        {
            error = "Lista ne dozvoljava brisanje.";
            return false;
        }

        contributions.Remove(contribution);
        error = string.Empty;
        return true;
    }
}
