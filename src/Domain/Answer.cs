using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain;

public class Answer
{
    public int Id { get; set; }
    public string Tekst { get; set; } = string.Empty;
    public bool JeTacan { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public Answer() { }

    public Answer(string tekst, bool jeTacan)
    {
        Tekst = tekst;
        JeTacan = jeTacan;
    }
}
