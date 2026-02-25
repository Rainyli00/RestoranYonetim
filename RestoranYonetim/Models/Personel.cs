using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Personel
{
    public int PersonelId { get; set; }

    public string AdSoyad { get; set; } = null!;

    public string? Telefon { get; set; }

    public string? Email { get; set; }

    public string? Adres { get; set; }

    public string KullaniciAdi { get; set; } = null!;

    public string SifreHash { get; set; } = null!;

    public int RolId { get; set; }

    public int HesapDurumId { get; set; }

    public int MesaiDurumId { get; set; }

    public DateTime? SonGirisTarihi { get; set; }

    public virtual ICollection<Gider> Gider { get; set; } = new List<Gider>();

    public virtual HesapDurum HesapDurum { get; set; } = null!;

    public virtual ICollection<IslemLog> IslemLog { get; set; } = new List<IslemLog>();

    public virtual MesaiDurum MesaiDurum { get; set; } = null!;

    public virtual ICollection<Odeme> Odeme { get; set; } = new List<Odeme>();

    public virtual Rol Rol { get; set; } = null!;

    public virtual ICollection<Siparis> Siparis { get; set; } = new List<Siparis>();
}
