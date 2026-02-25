using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class GiderKategori
{
    public int GiderKategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;

    public virtual ICollection<Gider> Gider { get; set; } = new List<Gider>();
}
