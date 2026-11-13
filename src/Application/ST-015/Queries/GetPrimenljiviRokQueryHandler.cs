using MediatR;
using Studium.Domain.ST015.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Studium.Application.ST015.Queries;

public class GetPrimenljiviRokQueryHandler : IRequestHandler<GetPrimenljiviRokQuery, DateTime>
{
    private readonly IRokoviRepository _repository;

    public GetPrimenljiviRokQueryHandler(IRokoviRepository repository)
    {
        _repository = repository;
    }

    public async Task<DateTime> Handle(GetPrimenljiviRokQuery request, CancellationToken cancellationToken)
    {
        // 1. Autorizacija na nivou resursa
        if (request.JeNastavnik)
        {
            var imaPravo = await _repository.DaLiNastavnikImaPravoNadZadatkomAsync(
                request.AkterId, request.ZadatakId, cancellationToken);

            if (!imaPravo)
            {
                throw new UnauthorizedAccessException("Nastavnik nema pravo pristupa ovom zadatku.");
            }
        }
        else
        {
            if (request.AkterId != request.StudentId)
            {
                throw new UnauthorizedAccessException("Student nema pravo da vidi rok drugog studenta.");
            }
        }

        // 2. Dobavljamo zadatak da bismo znali zajednički rok
        var zadatak = await _repository.GetZadatakAsync(request.ZadatakId, cancellationToken);
        if (zadatak == null)
        {
            throw new ArgumentException($"Zadatak sa ID {request.ZadatakId} nije pronađen.");
        }

        // 3. Proveravamo specifičan rok studenta
        var studentRok = await _repository.GetStudentRokAsync(request.StudentId, request.ZadatakId, cancellationToken);
        if (studentRok != null)
        {
            return studentRok.IzracunajPrimenljiviRok(zadatak.ZajednickiRok);
        }

        return zadatak.ZajednickiRok;
    }
}