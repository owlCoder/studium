namespace Domain;

public record ContentGroupContract(
    Guid ContentId,
    List<Guid> TargetGroupIds
);