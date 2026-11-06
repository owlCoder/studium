using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfferingId { get; set; }
    public Guid StudentId { get; set; }
    public bool IsActive { get; set; } = true;

    public void Terminate()
    {
        IsActive = false;
    }
}
