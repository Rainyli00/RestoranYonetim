using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Gider
{
    public int GiderId { get; set; }

    public int GiderKategoriId { get; set; }

    public int? PersonelId { get; set; }

    public string? Aciklama { get; set; }

    public decimal Tutar { get; set; }

    public DateTime GiderTarihi { get; set; }

    public virtual GiderKategori GiderKategori { get; set; } = null!;

    public virtual Personel? Personel { get; set; }
}
