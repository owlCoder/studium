using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Staff;

namespace Application.Home;

public static class RoleLabels
{
    public static string? For(string? roleName) => roleName switch
    {
        nameof(Responsibility.ResponsibleTeacher) => "Odgovorni nastavnik",
        nameof(Responsibility.Teacher) => "Nastavnik",
        nameof(Responsibility.Assistant) => "Asistent",
        _ => null
    };
}
