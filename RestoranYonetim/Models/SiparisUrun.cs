using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class SiparisUrun
{
    public int Id { get; set; }

    public int SiparisId { get; set; }

    public int UrunId { get; set; }

    public int Adet { get; set; }

    public decimal BirimFiyat { get; set; }

    public virtual Siparis Siparis { get; set; } = null!;

    public virtual Urun Urun { get; set; } = null!;
}
