using Domain.Academic;

namespace Application.Academic;

public sealed record ChangeOfferingStateCommand(
    Guid OfferingId,
    OfferingState NewState);
