using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Staff;

namespace Application.Engagements;

/// <summary>
/// Izvođenje na kom je korisnik angažovan, sa njegovom ulogom (US-7.2).
/// RoleName je ista uloga kao tekst ("ResponsibleTeacher", "Teacher", "Assistant").
/// </summary>
public sealed record TeachingOfferingDto(
    Guid OfferingId,
    string CourseName,
    string AcademicYear,
    Responsibility Role)
{
    public string RoleName => Role.ToString();
}
