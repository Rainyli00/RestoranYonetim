using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class SiparisDurum
{
    public int SiparisDurumId { get; set; }

    public string DurumAdi { get; set; } = null!;

    public virtual ICollection<Siparis> Siparis { get; set; } = new List<Siparis>();
}
