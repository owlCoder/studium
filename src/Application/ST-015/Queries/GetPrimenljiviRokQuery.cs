using MediatR;
using System;

namespace Studium.Application.ST015.Queries;

public record GetPrimenljiviRokQuery(Guid StudentId, Guid ZadatakId) : IRequest<DateTime>;