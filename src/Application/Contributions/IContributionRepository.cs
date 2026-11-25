using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;

namespace Application.Contributions;
public interface IContributionRepository
{
    Contribution? GetById(int id);
    void Save(Contribution contribution);
}
