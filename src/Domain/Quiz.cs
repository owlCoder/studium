using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;

namespace Domain;

public class Quiz
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<Question> Pitanja { get; private set; } = new List<Question>();
    public DateTime PocetakDostupnosti { get; set; }
    public DateTime KrajDostupnosti { get; set; }
    public int TrajanjeUMinutima { get; set; }
    public int MaxBrojPokusaja { get; set; }
    public StanjeKviza Stanje { get; private set; }

    public Quiz() { } // Za EF Core

    public Quiz(string naziv, DateTime pocetak, DateTime kraj, int trajanje, int maxPokusaja)
    {
        // Poslovno pravilo: Početak dostupnosti prethodi završetku; trajanje i broj pokušaja su pozitivni[span_3](start_span)[span_3](end_span)
        if (pocetak >= kraj)
            throw new ArgumentException("Početak dostupnosti mora biti prije završetka.");
        if (trajanje <= 0 || maxPokusaja <= 0)
            throw new ArgumentException("Trajanje i broj pokušaja moraju biti pozitivni brojevi.");

        Naziv = naziv;
        PocetakDostupnosti = pocetak;
        KrajDostupnosti = kraj;
        TrajanjeUMinutima = trajanje;
        MaxBrojPokusaja = maxPokusaja;
        Stanje = StanjeKviza.Nacrt;
    }

    public void DodajPitanje(Question pitanje)
    {
        if (Stanje != StanjeKviza.Nacrt)
        {
            throw new InvalidOperationException("Pitanja se mogu dodavati samo dok je kviz u statusu Nacrt.");
        }
        Pitanja.Add(pitanje);
    }

    public void ObjaviKviz()
    {
        if (Pitanja.Count == 0)
        {
            throw new InvalidOperationException("Kviz bez pitanja se ne može objaviti.");
        }
        Stanje = StanjeKviza.Objavljen;
    }
}
