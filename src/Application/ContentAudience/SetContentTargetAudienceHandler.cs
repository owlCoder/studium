using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.ContentAudience;

public class SetContentTargetAudienceHandler
{
    private readonly IContentTargetAudienceRepository _repository;
    private readonly IContentLookupForAudience _contentLookup;
    private readonly IGroupLookupForAudience _groupLookup;

    public SetContentTargetAudienceHandler(
        IContentTargetAudienceRepository repository,
        IContentLookupForAudience contentLookup,
        IGroupLookupForAudience groupLookup)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contentLookup = contentLookup ?? throw new ArgumentNullException(nameof(contentLookup));
        _groupLookup = groupLookup ?? throw new ArgumentNullException(nameof(groupLookup));
    }

    public async Task Handle(SetContentTargetAudienceCommand request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (request.ContentId == Guid.Empty)
        {
            throw new ArgumentException("ID sadržaja ne može biti prazan!", nameof(request.ContentId));
        }

        // 1. Provera postojanja sadržaja
        var contentExists = await _contentLookup.ExistsAsync(request.ContentId, cancellationToken);
        if (!contentExists)
        {
            throw new KeyNotFoundException("Navedeni nastavni sadržaj ne postoji.");
        }

        // 2. PROVERA AUTORIZACIJE: Da li ulogovani nastavnik sme da uređuje ovaj sadržaj?
        var canEdit = await _contentLookup.CanInstructorEditContentAsync(request.ContentId, request.InstructorId, cancellationToken);
        if (!canEdit)
        {
            throw new UnauthorizedAccessException("Nastavnik nema ovlašćenje da uređuje ovaj sadržaj ili izvođenje kursa.");
        }

        // 3. Poslovno pravilo: Ako nije za celo izvođenje, proveri grupe
        if (!request.IsForWholeCourseExecution)
        {
            if (request.TargetGroupIds == null || !request.TargetGroupIds.Any())
            {
                throw new ArgumentException("Prazan spisak grupa nije zamena za izbor celog izvođenja.", nameof(request.TargetGroupIds));
            }

            var courseExecutionId = await _contentLookup.GetCourseExecutionIdAsync(request.ContentId, cancellationToken);
            var groupsValid = await _groupLookup.AllGroupsBelongToCourseExecutionAsync(request.TargetGroupIds, courseExecutionId, cancellationToken);
            
            if (!groupsValid)
            {
                throw new InvalidOperationException("Izabrane grupe ne pripadaju istom izvođenju predmeta kao sadržaj.");
            }
        }

        var existingAudience = await _repository.GetByContentIdAsync(request.ContentId, cancellationToken);

        if (existingAudience != null)
        {
            existingAudience.IsForWholeCourseExecution = request.IsForWholeCourseExecution;
            existingAudience.TargetGroupIds = request.IsForWholeCourseExecution ? new List<Guid>() : request.TargetGroupIds!;
            await _repository.AddOrUpdateAsync(existingAudience, cancellationToken);
        }
        else
        {
            var newAudience = new Domain.Entities.ContentTargetAudience(
                request.ContentId,
                request.IsForWholeCourseExecution,
                request.TargetGroupIds
            );
            await _repository.AddOrUpdateAsync(newAudience, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }
}