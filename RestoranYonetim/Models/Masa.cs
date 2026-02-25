using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Masa
{
    public int MasaId { get; set; }

    public string MasaAdi { get; set; } = null!;

    public int DurumId { get; set; }

    public virtual MasaDurum Durum { get; set; } = null!;

    public virtual ICollection<Siparis> Siparis { get; set; } = new List<Siparis>();
}
