using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Odeme
{
    public int OdemeId { get; set; }

    public int SiparisId { get; set; }

    public int OdemeTuruId { get; set; }

    public int? PersonelId { get; set; }

    public decimal Tutar { get; set; }

    public DateTime Tarih { get; set; }

    public virtual OdemeTuru OdemeTuru { get; set; } = null!;

    public virtual Personel? Personel { get; set; }

    public virtual Siparis Siparis { get; set; } = null!;
}
