using MediatR;

namespace Studium.Application.ST015.Commands;

public sealed record ExtendIndividualDeadlineCommand(Guid StudentId, Guid ZadatakId, DateTime NoviRok) : IRequest<bool>;