using MediatR;
using Studium.Domain.ST015.Repositories;

namespace Studium.Application.ST015.Commands;

public sealed class ExtendIndividualDeadlineCommandHandler(IRokoviRepository _repository)
    : IRequestHandler<ExtendIndividualDeadlineCommand>
{
    public async Task Handle(ExtendIndividualDeadlineCommand request, CancellationToken ct)
    {
        if (!request.JeNastavnik)
            throw new UnauthorizedAccessException("Samo nastavnik može da produži pojedinačni rok.");

     var imaPravo = await _repository.DaLiNastavnikImaPravoNadZadatkomAsync(
    request.NastavnikId,
    request.ZadatakId,
    ct);

        if (!imaPravo)
            throw new UnauthorizedAccessException("Nastavnik nema ovlašćenje nad zadatim zadatkom ili studentom.");

        var studentRok = await _repository.GetStudentRokAsync(request.StudentId, request.ZadatakId, ct)
            ?? throw new ArgumentException("Rok za traženog studenta i zadatak nije pronađen.");

        studentRok.PostaviPojedinacniRok(request.NoviRok);
        await _repository.UpdateStudentRokAsync(studentRok, ct);
    }
}