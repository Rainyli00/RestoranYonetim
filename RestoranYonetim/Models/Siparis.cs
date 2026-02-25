using System;
using System.Collections.Generic;

namespace RestoranYonetim.Models;

public partial class Siparis
{
    public int SiparisId { get; set; }

    public int? MasaId { get; set; }

    public int? GarsonId { get; set; }

    public int SiparisDurumId { get; set; }

    public string? Notlar { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? KapatmaTarihi { get; set; }

    public virtual Personel? Garson { get; set; }

    public virtual Masa? Masa { get; set; }

    public virtual ICollection<Odeme> Odeme { get; set; } = new List<Odeme>();

    public virtual SiparisDurum SiparisDurum { get; set; } = null!;

    public virtual ICollection<SiparisUrun> SiparisUrun { get; set; } = new List<SiparisUrun>();
}
