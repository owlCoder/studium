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
        // 1. Dobavljamo zadatak da bismo znali zajednički rok
        var zadatak = await _repository.GetZadatakAsync(request.ZadatakId, cancellationToken);
        if (zadatak == null)
        {
            throw new ArgumentException($"Zadatak sa ID {request.ZadatakId} nije pronađen.");
        }

        // 2. Proveravamo da li student ima specifičan/produžen rok za ovaj zadatak
        var studentRok = await _repository.GetStudentRokAsync(request.StudentId, request.ZadatakId, cancellationToken);

        // 3. Ako ima, domen rešava šta ima prednost. Ako nema, vraćamo zajednički rok.
        if (studentRok != null)
        {
            return studentRok.IzracunajPrimenljiviRok(zadatak.ZajednickiRok);
        }

        return zadatak.ZajednickiRok;
    }
}