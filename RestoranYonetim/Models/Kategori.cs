using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Kategori
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;

    public int SiraNo { get; set; }

    public string? ResimUrl { get; set; }

    public virtual ICollection<Urun> Urun { get; set; } = new List<Urun>();
}
