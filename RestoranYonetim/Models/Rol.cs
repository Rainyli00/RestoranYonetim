using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Rol
{
    public int RolId { get; set; }

    public string RolAdi { get; set; } = null!;

    public virtual ICollection<Personel> Personel { get; set; } = new List<Personel>();
}
