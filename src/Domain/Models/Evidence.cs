using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;

public class Evidence
{
    public int Id { get; set; } = 0;
    public string FilePath { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime AddedAt {  get; set; } = DateTime.Now;

    public bool IsLink => !string.IsNullOrEmpty(Link);
}
