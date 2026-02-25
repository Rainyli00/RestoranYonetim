using System.Text.Json.Serialization;

namespace RestoranYonetim.Models
{
    /// <summary>
    /// AI Servisi - Yarin Satis Tahmini Modeli
    /// </summary>
    public class AiYarinTahmini
    {
        [JsonPropertyName("basari")]
        public bool Basari { get; set; }
        
        [JsonPropertyName("tarih")]
        public string? Tarih { get; set; }
        
        [JsonPropertyName("gun_adi")]
        public string? GunAdi { get; set; }
        
        [JsonPropertyName("tahmini_satis")]
        public int TahminiSatis { get; set; }
        
        [JsonPropertyName("tahmini_ciro")]
        public decimal TahminiCiro { get; set; }
        
        [JsonPropertyName("ortalama_satis_7gun")]
        public int OrtalamaSatis7gun { get; set; }
        
        [JsonPropertyName("ortalama_ciro_7gun")]
        public decimal OrtalamaCiro7gun { get; set; }
        
        [JsonPropertyName("satis_degisim_yuzde")]
        public double SatisDegisimYuzde { get; set; }
        
        [JsonPropertyName("ciro_degisim_yuzde")]
        public double CiroDegisimYuzde { get; set; }
        
        [JsonPropertyName("mesaj")]
        public string? Mesaj { get; set; }
        
        [JsonPropertyName("hata")]
        public string? Hata { get; set; }
    }

   
    /// AI Servisi - Stok Tukenme Tahmini Modeli
  
    public class AiStokTahmini
    {
        [JsonPropertyName("basari")]
        public bool Basari { get; set; }
        
        [JsonPropertyName("analiz_tarihi")]
        public string? AnalizTarihi { get; set; }
        
        [JsonPropertyName("toplam_urun")]
        public int ToplamUrun { get; set; }
        
        [JsonPropertyName("kritik_urun_sayisi")]
        public int KritikUrunSayisi { get; set; }
        
        [JsonPropertyName("tukenen_urun_sayisi")]
        public int TukenenUrunSayisi { get; set; }
        
        [JsonPropertyName("kritik_urunler")]
        public List<AiKritikUrun>? KritikUrunler { get; set; }
        
        [JsonPropertyName("mesaj")]
        public string? Mesaj { get; set; }
        
        [JsonPropertyName("hata")]
        public string? Hata { get; set; }
    }

    /// <summary>
    /// AI Servisi - Kritik Urun Modeli
    /// </summary>
    public class AiKritikUrun
    {
        [JsonPropertyName("urun_id")]
        public int UrunId { get; set; }
        
        [JsonPropertyName("urun_adi")]
        public string? UrunAdi { get; set; }
        
        [JsonPropertyName("kategori")]
        public string? Kategori { get; set; }
        
        [JsonPropertyName("mevcut_stok")]
        public int MevcutStok { get; set; }
        
        [JsonPropertyName("gunluk_satis_ortalama")]
        public double GunlukSatisOrtalama { get; set; }
        
        [JsonPropertyName("tahmini_kalan_gun")]
        public int? TahminiKalanGun { get; set; }
        
        [JsonPropertyName("tahmini_tukenme_tarihi")]
        public string? TahminiTukenmeTarihi { get; set; }
        
        [JsonPropertyName("aciliyet")]
        public string? Aciliyet { get; set; }
        
        [JsonPropertyName("aciliyet_renk")]
        public string? AciliyetRenk { get; set; }
    }
}
