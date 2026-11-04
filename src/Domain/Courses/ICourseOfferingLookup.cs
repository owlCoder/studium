using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Courses;

public interface ICourseOfferingLookup
{
    bool HasOfferings(Guid courseId);
}
