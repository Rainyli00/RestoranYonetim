using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Geribildirim
{
    public int YorumId { get; set; }

    public int TurId { get; set; }

    public int? UrunId { get; set; }

    public byte? Puan { get; set; }

    public string? Yorum { get; set; }

    public DateTime Tarih { get; set; }

    public virtual GeribildirimTuru Tur { get; set; } = null!;

    public virtual Urun? Urun { get; set; }
}
