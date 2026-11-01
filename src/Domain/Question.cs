using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain;

    public class Question
    {
        public int Id { get; set; }
        public string TekstPitanja { get; private set; } = string.Empty;
        public TipPitanja Tip { get; private set; }
        public string Oblast { get; private set; } = string.Empty;
        public List<string> Oznake { get; private set; } = new List<string>();
        public ICollection<Answer> Odgovori { get; private set; } = new List<Answer>();
        public bool JeIskoristenoUPokusaju { get; set; } // Poslovno pravilo: Pitanje korišćeno u pokušaju se ne menja[span_1](start_span)[span_1](end_span)

        public Question() { } // Za EF Core

        public Question(string tekstPitanja, TipPitanja tip, string oblast, List<Answer>? odgovori = null)
        {
            TekstPitanja = tekstPitanja;
            Tip = tip;
            Oblast = oblast;
            Odgovori = odgovori ?? new List<Answer>();
            Oznake = new List<string>();
            JeIskoristenoUPokusaju = false;

            ValidirajPitanje();
        }

        public void ValidirajPitanje()
        {
            if (Tip == TipPitanja.JedanTacenIzbor)
            {
                int brojTacnih = Odgovori.Count(o => o.JeTacan);
                if (brojTacnih != 1)
                {
                    throw new InvalidOperationException("Pitanje sa jednim tačnim izborom mora imati tačno jedan tačan odgovor.");
                }
            }
        }

        public void AzurirajTekst(string noviTekst)
        {
            if (JeIskoristenoUPokusaju)
            {
                throw new InvalidOperationException("Izmjena nije dozvoljena! Pitanje je već korišćeno u pokušaju kviza."); //[span_2](start_span)[span_2](end_span)
            }
            TekstPitanja = noviTekst;
        }
    }

