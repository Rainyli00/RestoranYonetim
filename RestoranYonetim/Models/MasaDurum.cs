using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class MasaDurum
{
    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = null!;

    public virtual ICollection<Masa> Masa { get; set; } = new List<Masa>();
}
