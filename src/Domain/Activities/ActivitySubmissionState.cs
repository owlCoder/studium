using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Activities;

/// <summary>Stanje predaje iz ugla pravila otvorene aktivnosti.</summary>
public enum ActivitySubmissionState
{
    Submitted, // važeća predaja
    Withdrawn  // student je povukao predaju — više ne važi
}
