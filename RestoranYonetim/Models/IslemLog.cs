using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class IslemLog
{
    public int LogId { get; set; }

    public int? PersonelId { get; set; }

    public int? IslemTuruId { get; set; }

    public string? IpAdresi { get; set; }

    public DateTime Zaman { get; set; }

    public string? IslemAciklama { get; set; }

    public virtual IslemTuru? IslemTuru { get; set; }

    public virtual Personel? Personel { get; set; }
}
