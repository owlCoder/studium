using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;
using Domain.Dashboard;

namespace Application.Activities;

/// <summary>
/// US-7.3: prava implementacija <see cref="IOpenActivityCounter"/>.
/// Svaki poziv ponovo čita aktivnosti i predaje i primenjuje <see cref="OpenActivityRule"/>,
/// a koje se vrste broje odlučuje Milenin registar (US-7.6);
/// ništa ne kešira, pa broj na početnoj strani prati stvarno stanje.
/// </summary>
public sealed class OpenActivityCounter : IOpenActivityCounter
{
    private readonly IActivityReader _activities;
    private readonly IActivitySubmissionReader _submissions;
    private readonly OpenActivityRule _rule;
    private readonly TimeProvider _time;

    public OpenActivityCounter(
        IActivityReader activities,
        IActivitySubmissionReader submissions,
        FeatureRegistry registry,
        TimeProvider time)
    {
        _activities = activities ?? throw new ArgumentNullException(nameof(activities));
        _submissions = submissions ?? throw new ArgumentNullException(nameof(submissions));
        _rule = new OpenActivityRule(registry);
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    public async Task<int> CountOpenAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        }
        if (offeringId == Guid.Empty)
        {
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var activities = await _activities.ListForOfferingAsync(offeringId, cancellationToken);
        var submissions = await _submissions.ListForStudentAsync(studentId, offeringId, cancellationToken);

       
        var ofOffering = activities.Where(a => a.OfferingId == offeringId);

        return _rule.CountOpen(ofOffering, studentId, submissions, _time.GetUtcNow());
    }
}
