using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoranYonetim.Models;
using RestoranYonetim.Helpers;

namespace RestoranYonetim.Controllers
{
    public class GarsonController : Controller
    {
        private readonly RestoranDbContext _context;

        public GarsonController(RestoranDbContext context)
        {
            _context = context;
        }

        // Yardımcı: Garson mı kontrol et
        private bool IsGarson()
        {
            var rolId = HttpContext.Session.GetInt32("RolId");
            return rolId == 1 || rolId == 2; // 1: Garson, 2: Yönetici
        }

        // ============================================================
        // 1. ANA SAYFA - MASA LİSTESİ
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index(bool tumunuGoster = true)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            ViewBag.AdSoyad = HttpContext.Session.GetString("AdSoyad");

            // Garson Bildirimlerini Çek
            var bildirimler = HttpContext.Session.GetString("GarsonBildirimleri");
            ViewBag.Bildirimler = bildirimler;

            var query = _context.Masa
                .Include(m => m.Durum)
                .Include(m => m.Siparis.Where(s => s.SiparisDurumId == 1 || s.SiparisDurumId == 2)) // Sadece Hazırlanıyor ve Servis Edildi
                    .ThenInclude(s => s.SiparisDurum)
                .Include(m => m.Siparis.Where(s => s.SiparisDurumId == 1 || s.SiparisDurumId == 2))
                    .ThenInclude(s => s.SiparisUrun)
                .AsQueryable();

            // Varsayılan: TÜM MASALAR gösterilir
            // tumunuGoster=false ise sadece DOLU (2) veya REZERVE (3) masaları göster
            if (!tumunuGoster)
            {
                query = query.Where(m => m.DurumId == 2 || m.DurumId == 3);
            }

            var masalar = await query.OrderBy(m => m.MasaAdi).ToListAsync();

            ViewBag.TumunuGoster = tumunuGoster;

            return View(masalar);
        }

        // ============================================================
        // 2. SİPARİŞ EKRANI - MASA DETAYI
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Siparis(int masaId)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            ViewBag.AdSoyad = HttpContext.Session.GetString("AdSoyad");

            var masa = await _context.Masa
                .Include(m => m.Durum)
                .FirstOrDefaultAsync(m => m.MasaId == masaId);

            if (masa == null)
            {
                TempData["Error"] = "Masa bulunamadı!";
                return RedirectToAction("Index");
            }

            // Bildirim varsa otomatik temizle (Garson masaya girdi, bildirim okundu)
            var bildirimler = HttpContext.Session.GetString("GarsonBildirimleri");
            if (!string.IsNullOrEmpty(bildirimler))
            {
                var liste = bildirimler.Split(';').ToList();
                // Bu masaya ait bildirimleri temizle (MasaId ile başlayanları sil)
                liste.RemoveAll(b => b.StartsWith(masaId.ToString() + "|"));
                
                if (liste.Any())
                {
                    HttpContext.Session.SetString("GarsonBildirimleri", string.Join(";", liste));
                }
                else
                {
                    HttpContext.Session.Remove("GarsonBildirimleri");
                }
            }

            // Masanın aktif siparişini kontrol et
            var aktifSiparis = await _context.Siparis
                .Include(s => s.SiparisDurum)
                .Include(s => s.SiparisUrun)
                .ThenInclude(su => su.Urun)
                .FirstOrDefaultAsync(s => s.MasaId == masaId && (s.SiparisDurumId == 1 || s.SiparisDurumId == 2)); // Hazırlanıyor veya Servis Edildi

            // Menü kategorileri ve ürünler (Sadece aktif olanlar, stok kontrolü view'da)
            var kategoriler = await _context.Kategori
                .Include(k => k.Urun.Where(u => u.AktifMi == 1))
                .OrderBy(k => k.SiraNo)
                .ToListAsync();

            ViewBag.Masa = masa;
            ViewBag.AktifSiparis = aktifSiparis;
            ViewBag.Kategoriler = kategoriler;
            ViewBag.GarsonId = HttpContext.Session.GetInt32("PersonelId");

            return View();
        }

        // ============================================================
        // 3. ÜRÜN EKLE (+1) - STOK KONTROLÜ İLE
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UrunEkle(int masaId, int urunId)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            try
            {
                int? garsonId = HttpContext.Session.GetInt32("PersonelId");

                // A. STOK KONTROLÜ
                var urun = await _context.Urun.FindAsync(urunId);
                if (urun == null)
                {
                    TempData["Error"] = "Ürün bulunamadı!";
                    return RedirectToAction("Siparis", new { masaId });
                }

                if (urun.Stok < 1)
                {
                    TempData["Error"] = $"{urun.UrunAdi} stoğu tükendi!";
                    return RedirectToAction("Siparis", new { masaId });
                }

                // B. SİPARİŞ OLUŞTUR / BUL
                var siparis = await _context.Siparis
                    .FirstOrDefaultAsync(s => s.MasaId == masaId && (s.SiparisDurumId == 1 || s.SiparisDurumId == 2)); // Hazırlanıyor veya Servis Edildi

                if (siparis == null)
                {
                    // Yeni sipariş oluştur
                    siparis = new Siparis
                    {
                        MasaId = masaId,
                        GarsonId = garsonId,
                        SiparisDurumId = 1, // Hazırlanıyor
                        OlusturmaTarihi = DateTime.Now
                    };
                    _context.Siparis.Add(siparis);
                    await _context.SaveChangesAsync();

                    // Masayı "Dolu" yap
                    var masa = await _context.Masa.FindAsync(masaId);
                    if (masa != null)
                    {
                        masa.DurumId = 2; // Dolu
                        await _context.SaveChangesAsync();
                        
                        // Log kaydet - Yeni sipariş oluşturuldu
                        await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.SiparisOlusturma,
                            $"Yeni sipariş oluşturuldu: {masa.MasaAdi} (Sipariş ID: {siparis.SiparisId})");
                    }
                }

                // C. SEPETE EKLE
                var sepetUrun = await _context.SiparisUrun
                    .FirstOrDefaultAsync(su => su.SiparisId == siparis.SiparisId && su.UrunId == urunId);

                if (sepetUrun != null)
                {
                    sepetUrun.Adet++; // Miktarı artır
                }
                else
                {
                    _context.SiparisUrun.Add(new SiparisUrun
                    {
                        SiparisId = siparis.SiparisId,
                        UrunId = urunId,
                        Adet = 1,
                        BirimFiyat = urun.Fiyat
                    });
                }

                // D. STOK DÜŞÜR
                urun.Stok -= 1;

                if (urun.Stok == 0)
                {
                    TempData["Warning"] = $"{urun.UrunAdi} ürününün stoğu tükendi!";
                }

                await _context.SaveChangesAsync();
                
                // Log kaydet - Ürün eklendi
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.SiparisUrunEkleme,
                    $"Siparişe ürün eklendi: {urun.UrunAdi} (Masa ID: {masaId})");
                
                TempData["Success"] = $"{urun.UrunAdi} sepete eklendi!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
            }

            return RedirectToAction("Siparis", new { masaId });
        }

        // ============================================================
        // 4. ÜRÜN ÇIKAR (-1) - STOK İADESİ İLE
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UrunCikar(int masaId, int urunId)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            try
            {
                var siparis = await _context.Siparis
                    .FirstOrDefaultAsync(s => s.MasaId == masaId && (s.SiparisDurumId == 1 || s.SiparisDurumId == 2)); // Hazırlanıyor veya Servis Edildi

                if (siparis != null)
                {
                    var sepetUrun = await _context.SiparisUrun
                        .FirstOrDefaultAsync(su => su.SiparisId == siparis.SiparisId && su.UrunId == urunId);

                    if (sepetUrun != null)
                    {
                        // A. STOK İADESİ (Geri yerine koy)
                        var urun = await _context.Urun.FindAsync(urunId);
                        if (urun != null)
                        {
                            urun.Stok += 1;
                        }

                        // B. SEPETTEN DÜŞME
                        if (sepetUrun.Adet > 1)
                        {
                            sepetUrun.Adet--;
                        }
                        else
                        {
                            _context.SiparisUrun.Remove(sepetUrun);
                        }

                        await _context.SaveChangesAsync();
                        
                        // Log kaydet - Ürün çıkarıldı
                        await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.SiparisUrunCikarma,
                            $"Siparişten ürün çıkarıldı: {urun?.UrunAdi ?? "Bilinmeyen"} (Masa ID: {masaId})");
                        
                        TempData["Success"] = "Ürün sepetten çıkarıldı!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
            }

            return RedirectToAction("Siparis", new { masaId });
        }

        // ============================================================
        // 5. SİPARİŞ NOTU KAYDET
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> NotKaydet(int siparisId, string notlar, int masaId)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            var siparis = await _context.Siparis.FindAsync(siparisId);
            if (siparis != null)
            {
                siparis.Notlar = notlar;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Not kaydedildi.";
            }

            return RedirectToAction("Siparis", new { masaId });
        }

        // ============================================================
        // 6. SİPARİŞ DURUMU GÜNCELLE
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> SiparisDurumGuncelle(int siparisId, int yeniDurum)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            try
            {
                var siparis = await _context.Siparis.FindAsync(siparisId);
                if (siparis == null)
                {
                    TempData["Error"] = "Sipariş bulunamadı!";
                    return RedirectToAction("Index");
                }

                siparis.SiparisDurumId = yeniDurum;
                await _context.SaveChangesAsync();

                // Log kaydet - Sipariş durumu güncellendi
                var durumAdi = yeniDurum == 2 ? "Servis Edildi" : yeniDurum == 3 ? "Tamamlandı" : "Güncellendi";
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.SiparisDurumGuncelleme,
                    $"Sipariş durumu güncellendi: {durumAdi} (Sipariş ID: {siparisId})");

                TempData["Success"] = "Sipariş durumu güncellendi!";
                return RedirectToAction("Siparis", new { masaId = siparis.MasaId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ============================================================
        // 7. ÖDEME AL VE HESAP KAPAT
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> OdemeAl(int siparisId, int odemeTuruId)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            try
            {
                var siparis = await _context.Siparis
                    .Include(s => s.SiparisUrun)
                    .FirstOrDefaultAsync(s => s.SiparisId == siparisId);

                if (siparis == null)
                {
                    TempData["Error"] = "Sipariş bulunamadı!";
                    return RedirectToAction("Index");
                }

                // Toplam tutarı hesapla
                decimal toplamTutar = siparis.SiparisUrun.Sum(su => su.Adet * su.BirimFiyat);

                // Ödeme kaydı oluştur
                var odeme = new Odeme
                {
                    SiparisId = siparisId,
                    Tutar = toplamTutar,
                    OdemeTuruId = odemeTuruId,
                    PersonelId = HttpContext.Session.GetInt32("PersonelId"),
                    Tarih = DateTime.Now
                };
                _context.Odeme.Add(odeme);

                // Sipariş durumunu "Tamamlandı" yap
                siparis.SiparisDurumId = 3; // 3: Tamamlandı
                siparis.KapatmaTarihi = DateTime.Now;

                // Masa durumunu "Boş" yap
                var masa = await _context.Masa.FindAsync(siparis.MasaId);
                if (masa != null)
                {
                    masa.DurumId = 1; // 1: Boş
                }

                await _context.SaveChangesAsync();
                
                // Log kaydet - Ödeme alındı
                var odemeTuru = odemeTuruId == 1 ? "Nakit" : "Kredi Kartı";
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.OdemeAlma,
                    $"Ödeme alındı: {toplamTutar:C2} ({odemeTuru}) - Sipariş ID: {siparisId}");
                
                TempData["Success"] = $"✅ Ödeme alındı! Tutar: {toplamTutar:C2}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ============================================================
        // 8. AKTİF SİPARİŞLER (TÜM GARSONLAR)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> AktifSiparisler()
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            ViewBag.AdSoyad = HttpContext.Session.GetString("AdSoyad");

            // Tüm aktif siparişleri çek
            var aktifSiparisler = await _context.Siparis
                .Include(s => s.Masa)
                .Include(s => s.Garson)
                .Include(s => s.SiparisDurum)
                .Include(s => s.SiparisUrun)
                .ThenInclude(su => su.Urun)
                .Where(s => s.SiparisDurumId == 1 || s.SiparisDurumId == 2) // Hazırlanıyor veya Servis Edildi
                .OrderByDescending(s => s.OlusturmaTarihi)
                .ToListAsync();

            return View(aktifSiparisler);
        }

        // ============================================================
        // 9. SİPARİŞ İPTAL ET
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> SiparisIptal(int siparisId)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            try
            {
                var siparis = await _context.Siparis
                    .Include(s => s.SiparisUrun)
                    .FirstOrDefaultAsync(s => s.SiparisId == siparisId);

                if (siparis == null)
                {
                    TempData["Error"] = "Sipariş bulunamadı!";
                    return RedirectToAction("Index");
                }

                // STOK İADESİ (Tüm ürünleri geri koy)
                foreach (var su in siparis.SiparisUrun)
                {
                    var urun = await _context.Urun.FindAsync(su.UrunId);
                    if (urun != null)
                    {
                        urun.Stok += su.Adet; // Stoğu geri ekle
                    }
                }

                // Sipariş durumunu "İptal Edildi" yap
                siparis.SiparisDurumId = 4; // 4: İptal Edildi

                // Masa durumunu "Boş" yap
                var masa = await _context.Masa.FindAsync(siparis.MasaId);
                if (masa != null)
                {
                    masa.DurumId = 1; // 1: Boş
                }

                await _context.SaveChangesAsync();
                
                // Log kaydet - Sipariş iptal edildi
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.SiparisIptal,
                    $"Sipariş iptal edildi (Sipariş ID: {siparisId}, İade edilen ürün sayısı: {siparis.SiparisUrun.Count})");
                
                TempData["Success"] = "Sipariş iptal edildi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ============================================================
        // 10. BİLDİRİM SAYISI (AJAX için JSON döndür)
        // ============================================================
        [HttpGet]
        public IActionResult BildirimSayisi()
        {
            var bildirimler = HttpContext.Session.GetString("GarsonBildirimleri");
            var sayi = string.IsNullOrEmpty(bildirimler) ? 0 : bildirimler.Split(';').Length;
            
            return Json(new { sayi, bildirimler });
        }

        // ============================================================
        // 11. MASA REZERVASYON İŞLEMİ (EKLE/KALDIR)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> MasaRezervIslem(int masaId, bool rezerve)
        {
            if (!IsGarson()) return RedirectToAction("Login", "Auth");

            var masa = await _context.Masa.FindAsync(masaId);
            if (masa != null)
            {
                if (rezerve)
                {
                    masa.DurumId = 1; // Boş
                    TempData["Success"] = $"Masa {masa.MasaAdi} rezervasyonu kaldırıldı.";

                    // Log kaydet - Rezervasyon kaldırıldı
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.MasaRezervasyon,
                        $"Masa rezervasyonu kaldırıldı: {masa.MasaAdi}");
                }
                else
                {
                    masa.DurumId = 3; // Rezerve
                    TempData["Success"] = $"Masa {masa.MasaAdi} rezerve edildi.";

                    // Log kaydet - Masa rezerve edildi
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.MasaRezervasyon,
                        $"Masa rezerve edildi: {masa.MasaAdi}");
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Error"] = "Masa bulunamadı!";
            }
            return RedirectToAction("Siparis", new { masaId });
        }
    }
}



























