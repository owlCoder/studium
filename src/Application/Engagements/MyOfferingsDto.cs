using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Enrollments;

namespace Application.Engagements;

/// <summary>
/// Obe liste prijavljenog korisnika: „Predajem” (Teaching) i „Upisan sam” (Enrolled).
/// Isto izvođenje se pojavljuje najviše jednom u obe liste zajedno.
/// </summary>
public sealed record MyOfferingsDto(
    IReadOnlyList<TeachingOfferingDto> Teaching,
    IReadOnlyList<EnrolledOfferingDto> Enrolled);
