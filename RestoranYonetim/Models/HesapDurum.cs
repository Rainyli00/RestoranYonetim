using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class HesapDurum
{
    public int HesapDurumId { get; set; }

    public string DurumAdi { get; set; } = null!;

    public virtual ICollection<Personel> Personel { get; set; } = new List<Personel>();
}
