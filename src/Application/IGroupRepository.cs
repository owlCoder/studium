using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application;

public interface IGroupRepository //9
{
    Task<Domain.Entities.Group?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
    Domain.Entities.Group group,
    CancellationToken cancellationToken);
}
