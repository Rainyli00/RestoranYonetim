using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class OdemeTuru
{
    public int OdemeTuruId { get; set; }

    public string OdemeTuruAdi { get; set; } = null!;

    public virtual ICollection<Odeme> Odeme { get; set; } = new List<Odeme>();
}
