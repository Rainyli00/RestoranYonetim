using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class IslemTuru
{
    public int IslemTuruId { get; set; }

    public string TurAdi { get; set; } = null!;

    public virtual ICollection<IslemLog> IslemLog { get; set; } = new List<IslemLog>();
}
