using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class GeribildirimTuru
{
    public int TurId { get; set; }

    public string TurAdi { get; set; } = null!;

    public virtual ICollection<Geribildirim> Geribildirim { get; set; } = new List<Geribildirim>();
}
