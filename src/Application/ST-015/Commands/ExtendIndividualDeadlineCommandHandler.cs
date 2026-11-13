using MediatR;
using Studium.Domain.ST015.Repositories;

namespace Studium.Application.ST015.Commands;

internal sealed class ExtendIndividualDeadlineCommandHandler(IRokoviRepository _repository) 
    : IRequestHandler<ExtendIndividualDeadlineCommand, bool>
{
    public async Task<bool> Handle(ExtendIndividualDeadlineCommand request, CancellationToken ct)
    {
        var studentRok = await _repository.GetStudentRokAsync(request.StudentId, request.ZadatakId, ct);
        if (studentRok is null) return false;

        studentRok.PostaviPojedinacniRok(request.NoviRok);
        await _repository.UpdateStudentRokAsync(studentRok);
        return true;
    }
}