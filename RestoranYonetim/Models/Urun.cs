using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Urun
{
    public int UrunId { get; set; }

    public int? KategoriId { get; set; }

    public string UrunAdi { get; set; } = null!;

    public decimal Fiyat { get; set; }

    public int Stok { get; set; }

    public byte AktifMi { get; set; }

    public string? ResimUrl { get; set; }

    public virtual ICollection<Geribildirim> Geribildirim { get; set; } = new List<Geribildirim>();

    public virtual Kategori? Kategori { get; set; }

    public virtual ICollection<SiparisUrun> SiparisUrun { get; set; } = new List<SiparisUrun>();
}
