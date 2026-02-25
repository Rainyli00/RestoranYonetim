using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoranYonetim.Helpers;
using RestoranYonetim.Models;
using System.Text.Json;

namespace RestoranYonetim.Controllers
{
    public class AdminController : Controller
    {
        private readonly RestoranDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string AI_SERVICE_URL = "http://localhost:8000";

        public AdminController(RestoranDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // --- AI SERVÝSÝ YARDIMCI METOTLARI ---
        private async Task<T?> GetAiDataAsync<T>(string endpoint) where T : class
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5); // 5 saniye timeout
                var response = await client.GetAsync($"{AI_SERVICE_URL}{endpoint}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // JSON'u T tipine deserialize et yani dönüþtürür
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout oldu, sessizce devam et
            }
            catch (HttpRequestException)
            {
                // AI servisi çalýþmýyor, sessizce devam et
            }
            catch (Exception)
            {
                // Diðer hatalar, sessizce devam et
            }
            return null;
        }

        // --- YARDIMCI ---
        private bool IsAdmin()
        {
            return HttpContext.Session.GetInt32("RolId") == 2;
        }

        // Bildirim verilerini ViewBag'e ekle (Her sayfada kullanýlacak)
        private async Task LoadNotifications()
        {
            // Kritik Stok (10'dan az olanlar)
            var kritikStokUrunler = await _context.Urun
                .Where(u => u.Stok < 10 && u.AktifMi == 1)
                .ToListAsync();
            var kritikStok = kritikStokUrunler.Count;

            // Stoðu 0 olan ürünler
            var stoguBitenUrunler = kritikStokUrunler.Where(u => u.Stok == 0).ToList();
            var stoguBitenUrunSayisi = stoguBitenUrunler.Count;

            // "Diðer" Kategorisindeki Ürünler
            var digerKategori = await _context.Kategori
                .Include(k => k.Urun)
                .FirstOrDefaultAsync(k => k.KategoriAdi.ToLower() == "diðer" || k.KategoriAdi.ToLower() == "diger");
            var digerKategorisiUrunSayisi = digerKategori?.Urun.Count(u => u.AktifMi == 1) ?? 0;

            ViewBag.KritikStok = kritikStok;
            ViewBag.KritikStokUrunler = kritikStokUrunler;
            ViewBag.StoguBitenUrunler = stoguBitenUrunler;
            ViewBag.StoguBitenUrunSayisi = stoguBitenUrunSayisi;
            ViewBag.DigerKategorisiUrunSayisi = digerKategorisiUrunSayisi;
            ViewBag.DigerKategoriId = digerKategori?.KategoriId;
        }

        // ============================================================
        // 1. DASHBOARD (KOKPÝT - ANA SAYFA)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // GÜVENLÝK: Sadece "Yönetici" girebilir
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            // Patronun adýný al (Layout'ta göstermek için)
            ViewBag.AdSoyad = HttpContext.Session.GetString("AdSoyad");

            var bugun = DateTime.Today;

            // --- VERÝTABANI SORGULARI ---

            // 1. Günlük Ciro (Bugün tarihli ödemelerin toplamý)
            var ciro = await _context.Odeme
                .Where(o => o.Tarih.Date == bugun)
                .SumAsync(o => (decimal?)o.Tutar) ?? 0;

            // 2. Bugün Açýlan Sipariþ Sayýsý
            var siparisSayisi = await _context.Siparis
                .Where(s => s.OlusturmaTarihi.Date == bugun)
                .CountAsync();

            // 3. Masa Durumu (Dolu / Toplam)
            var doluMasa = await _context.Masa.CountAsync(m => m.DurumId == 2);
            var toplamMasa = await _context.Masa.CountAsync();

            // --- Verileri ViewBag'e Koy ---
            ViewBag.Ciro = ciro;
            ViewBag.SiparisSayisi = siparisSayisi;
            ViewBag.MasaDurumu = $"{doluMasa} / {toplamMasa}";
            // KritikStok, DigerKategorisiUrunSayisi, DigerKategoriId zaten LoadNotifications() tarafýndan yüklendi

            return View();
        }

        // ------------------------------------------------------------
        // 2. PERSONEL ÝÞLEMLERÝ
        // ------------------------------------------------------------

        // A. LÝSTELEME SAYFASI
        [HttpGet]
        public async Task<IActionResult> Personel(int? durum, int? rol, string arama, int sayfa = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            int sayfaBasinaKayit = 8; // Her sayfada 8 kayýt

            // Personelleri Rolleri ve Durumlarýyla beraber çekiyoruz
            var query = _context.Personel
                .Include(p => p.Rol)
                .Include(p => p.HesapDurum)
                .Include(p => p.MesaiDurum)
                .AsQueryable();

            // Filtreleme: Durum parametresi varsa uygula
            if (durum.HasValue)
            {
                query = query.Where(p => p.HesapDurumId == durum.Value);
            }

            // Rol filtreleme
            if (rol.HasValue)
            {
                query = query.Where(p => p.RolId == rol.Value);
            }

            // Arama (Tüm kolonlarda)
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim().ToLower();
                query = query.Where(p =>
                    p.AdSoyad.ToLower().Contains(arama) ||
                    p.KullaniciAdi.ToLower().Contains(arama) ||
                    (p.Telefon != null && p.Telefon.ToLower().Contains(arama)) ||
                    (p.Email != null && p.Email.ToLower().Contains(arama)) ||
                    (p.Adres != null && p.Adres.ToLower().Contains(arama))
                );
            }

            // Toplam kayýt sayýsý
            var toplamKayit = await query.CountAsync();
            var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            // Sayfa doðrulama
            if (sayfa < 1) sayfa = 1;
            if (sayfa > toplamSayfa && toplamSayfa > 0) sayfa = toplamSayfa;

            var personeller = await query
                .OrderBy(p => p.AdSoyad)
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .ToListAsync();

            // Ekleme Modal'ýndaki Dropdown için Rolleri de gönderelim
            ViewBag.Roller = await _context.Rol.ToListAsync();

            // Aktif filtreleri View'e gönder
            ViewBag.AktifDurum = durum;
            ViewBag.AktifRol = rol;
            ViewBag.Arama = arama;

            // Sayfalama bilgileri
            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.ToplamKayit = toplamKayit;

            return View(personeller);
        }

        // B. YENÝ PERSONEL EKLEME
        [HttpPost]
        public async Task<IActionResult> PersonelEkle(string adSoyad, string kullaniciAdi, string sifre, string telefon, string email, string adres, int rolId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            try
            {
                // Girilen þifreyi hashle
                string hashlenmisifre = Sifreleme.Sifrele(sifre);

                var yeniPersonel = new Personel
                {
                    AdSoyad = adSoyad,
                    KullaniciAdi = kullaniciAdi,
                    Telefon = telefon,
                    Email = email,
                    Adres = adres,
                    RolId = rolId,
                    SifreHash = hashlenmisifre,
                    HesapDurumId = 1, // 1: Aktif
                    MesaiDurumId = 1 // 1: Mesai Dýþý (varsayýlan)
                };

                _context.Personel.Add(yeniPersonel);
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.PersonelEkleme, $"Yeni personel eklendi: {adSoyad} (Rol: {rolId})");

                TempData["Success"] = $"{adSoyad} baþarýyla eklendi!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Personel eklenirken bir hata oluþtu.";
            }

            return RedirectToAction("Personel");
        }

        // C. PERSONEL SÝLME (PASÝFE ALMA veya FÝZÝKSEL SÝLME)
        [HttpPost]
        public async Task<IActionResult> PersonelSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var p = await _context.Personel.FindAsync(id);
            if (p == null)
            {
                TempData["Error"] = "Personel bulunamadý.";
                return RedirectToAction("Personel");
            }

            string adSoyad = p.AdSoyad;

            try
            {
                // 1. Personel geçmiþ sipariþlerde görev almýþ mý kontrol et (Garson olarak)
                var siparislerdeGorevAldiMi = await _context.Siparis
                    .AnyAsync(s => s.GarsonId == id);

                // 2. Personel gider kaydý oluþturmuþ mu kontrol et
                var giderKaydiVarMi = await _context.Gider
                    .AnyAsync(g => g.PersonelId == id);

                if (siparislerdeGorevAldiMi || giderKaydiVarMi)
                {
                    // GEÇMÝÞ KAYITLARDA VAR › Pasife al (Ýþten Çýkarýldý)
                    p.HesapDurumId = 4; // 4: Ýþten Ayrýldý
                    p.MesaiDurumId = 1; // 1: Mesai Dýþý (sistemden at)
                    
                    await _context.SaveChangesAsync();

                    // Log kaydet
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.PersonelSilme, $"Personel pasife alýndý: {adSoyad} (ID: {id})");

                    TempData["Success"] = $"{adSoyad} iþten çýkarýldý (hesap pasife alýndý).";
                }
                else
                {
                    // GEÇMÝÞTE HÝÇ KULLANILMAMIÞ › Fiziksel olarak sil
                    _context.Personel.Remove(p);
                    await _context.SaveChangesAsync();

                    // Log kaydet
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.PersonelSilme, $"Personel silindi: {adSoyad} (ID: {id})");

                    TempData["Success"] = $"{adSoyad} baþarýyla silindi.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"? Personel silinirken hata oluþtu: {ex.Message}";
            }

            return RedirectToAction("Personel");
        }

        // D. PERSONEL GÜNCELLEME
        [HttpPost]
        public async Task<IActionResult> PersonelGuncelle(int personelId, string adSoyad, string kullaniciAdi, string yeniSifre, string telefon, string email, string adres, int rolId, int hesapDurumId, int mesaiDurumId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var p = await _context.Personel.FindAsync(personelId);
            if (p != null)
            {
                p.AdSoyad = adSoyad;
                p.KullaniciAdi = kullaniciAdi;
                p.Telefon = telefon;
                p.Email = email;
                p.Adres = adres;
                p.RolId = rolId;
                p.HesapDurumId = hesapDurumId;
                p.MesaiDurumId = mesaiDurumId;

                // Eðer yeni þifre girildiyse güncelle, boþsa eski þifre kalsýn
                if (!string.IsNullOrEmpty(yeniSifre))
                {
                    p.SifreHash = Sifreleme.Sifrele(yeniSifre);
                }

                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.PersonelGuncelleme, $"Personel güncellendi: {adSoyad} (ID: {personelId})");

                TempData["Success"] = $"{adSoyad} bilgileri güncellendi!";
            }
            else
            {
                TempData["Error"] = "Personel bulunamadý.";
            }

            return RedirectToAction("Personel");
        }


        // ============================================================
        // 3. SÝPARÝÞ GEÇMÝÞÝ 
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Siparisler(DateTime? baslangic, DateTime? bitis, int? durumId, string arama, string siralama, int sayfa = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            await LoadNotifications();

            int sayfaBasinaKayit = 8;

            if (!bitis.HasValue) bitis = DateTime.Today;

            var query = _context.Siparis
                .Include(s => s.Masa)
                .Include(s => s.Garson)
                .Include(s => s.SiparisDurum)
                .Include(s => s.SiparisUrun).ThenInclude(su => su.Urun)
                .Include(s => s.Odeme).ThenInclude(o => o.OdemeTuru)
                .AsQueryable();

            // 1. & 2. Tarih Filtreleri
            if (baslangic.HasValue)
                query = query.Where(s => s.OlusturmaTarihi >= baslangic.Value);

            var bitisSonu = bitis.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(s => s.OlusturmaTarihi <= bitisSonu);

            // 3. Durum Filtresi
            if (durumId.HasValue)
                query = query.Where(s => s.SiparisDurumId == durumId);

            // 4. Arama Filtresi
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim().ToLower();
                bool aramaNumeric = int.TryParse(arama, out int siparisId);

                query = query.Where(s =>
                    (aramaNumeric && s.SiparisId == siparisId) ||
                    (s.Masa != null && s.Masa.MasaAdi.ToLower().Contains(arama)) ||
                    (s.Garson != null && s.Garson.AdSoyad.ToLower().Contains(arama)) ||
                    (s.Notlar != null && s.Notlar.ToLower().Contains(arama)) ||
                    (s.SiparisDurum != null && s.SiparisDurum.DurumAdi.ToLower().Contains(arama))
                );
            }

            // 5. SIRALAMA 
            // Varsayýlan sýralama "tarih-yeni"
            if (string.IsNullOrEmpty(siralama)) siralama = "tarih-yeni";

            query = siralama.ToLower() switch
            {
                "tutar-azalan" => query.OrderByDescending(s => s.SiparisUrun.Sum(u => u.Adet * u.BirimFiyat)),
                "tutar-artan" => query.OrderBy(s => s.SiparisUrun.Sum(u => u.Adet * u.BirimFiyat)),
                "tarih-eski" => query.OrderBy(s => s.OlusturmaTarihi),
                "tarih-yeni" or _ => query.OrderByDescending(s => s.OlusturmaTarihi) // Varsayýlan
            };

            // Toplam kayýt sayýsý
            var toplamKayit = await query.CountAsync();
            var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            if (sayfa < 1) sayfa = 1;
            if (sayfa > toplamSayfa && toplamSayfa > 0) sayfa = toplamSayfa;

            // SAYFALAMA 
            var siparisler = await query
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .ToListAsync();

            // Viewbag'ler
            ViewBag.Baslangic = baslangic;
            ViewBag.Bitis = bitis;
            ViewBag.AktifDurum = durumId;
            ViewBag.Durumlar = await _context.SiparisDurum.ToListAsync();
            ViewBag.Arama = arama;
            ViewBag.Siralama = siralama; // Seçilen sýralamayý View'da tutmak için

            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.ToplamKayit = toplamKayit;

            return View(siparisler);
        }

        // ============================================================
        // 4. KATEGORÝ VE ÜRÜN YÖNETÝMÝ
        // ============================================================

        // --- KATEGORÝLER SAYFASI ---
        [HttpGet]
        public async Task<IActionResult> Kategoriler(string arama, int sayfa = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            int sayfaBasinaKayit = 12; // Her sayfada 12 kategori

            var query = _context.Kategori
                .Include(k => k.Urun)
                .AsQueryable();

            // Arama filtresi
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim().ToLower();
                query = query.Where(k => 
                    k.KategoriAdi.ToLower().Contains(arama) ||
                    k.SiraNo.ToString().Contains(arama)
                );
            }

            // Toplam kayýt sayýsý
            var toplamKayit = await query.CountAsync();
            var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            // Sayfa doðrulama
            if (sayfa < 1) sayfa = 1;
            if (sayfa > toplamSayfa && toplamSayfa > 0) sayfa = toplamSayfa;

            // Kategorileri sýra numarasýna göre getir
            var kategoriler = await query
                .OrderBy(k => k.SiraNo)
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .ToListAsync();

            // Sayfalama ve filtreleme bilgileri
            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.ToplamKayit = toplamKayit;
            ViewBag.Arama = arama;

            return View(kategoriler);
        }

        [HttpPost]
        public async Task<IActionResult> KategoriEkle(string ad, int sira, string url)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            try
            {
                _context.Kategori.Add(new Kategori { KategoriAdi = ad, SiraNo = sira, ResimUrl = url });
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.KategoriEkleme, $"Yeni kategori eklendi: {ad}");

                TempData["Success"] = "Kategori eklendi.";
            }
            catch { TempData["Error"] = "Hata oluþtu."; }
            return RedirectToAction("Kategoriler");
        }

        [HttpPost]
        public async Task<IActionResult> KategoriGuncelle(int id, string ad, int sira, string url)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var kat = await _context.Kategori.FindAsync(id);
            if (kat != null)
            {
                kat.KategoriAdi = ad;
                kat.SiraNo = sira;
                kat.ResimUrl = url;
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.KategoriGuncelleme, $"Kategori güncellendi: {ad} (ID: {id})");

                TempData["Success"] = "Kategori güncellendi.";
            }
            return RedirectToAction("Kategoriler");
        }

        [HttpPost]
        public async Task<IActionResult> KategoriUrunleriDigereTasi(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var kat = await _context.Kategori
                .Include(k => k.Urun)
                .FirstOrDefaultAsync(k => k.KategoriId == id);

            if (kat != null)
            {
                // "Diðer" kategorisini kontrol et
                if (kat.KategoriAdi.ToLower() == "diðer" || kat.KategoriAdi.ToLower() == "diger")
                {
                    TempData["Error"] = "?? 'Diðer' kategorisindeki ürünler baþka yere taþýnamaz!";
                    return RedirectToAction("Kategoriler");
                }

                try
                {
                    // "Diðer" kategorisini bul veya oluþtur
                    var digerKategori = await _context.Kategori
                        .FirstOrDefaultAsync(k => k.KategoriAdi.ToLower() == "diðer" || k.KategoriAdi.ToLower() == "diger");
                    
                    if (digerKategori == null)
                    {
                        // "Diðer" kategorisi yoksa oluþtur
                        digerKategori = new Kategori 
                        { 
                            KategoriAdi = "Diðer", 
                            SiraNo = 9999,
                            ResimUrl = null 
                        };
                        _context.Kategori.Add(digerKategori);
                        await _context.SaveChangesAsync();
                    }

                    int urunSayisi = kat.Urun.Count;

                    if (urunSayisi > 0)
                    {
                        // Ürünleri "Diðer" kategorisine taþý
                        foreach (var urun in kat.Urun)
                        {
                            urun.KategoriId = digerKategori.KategoriId;
                        }
                        await _context.SaveChangesAsync();

                        // Log kaydet
                        await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.KategoriGuncelleme, $"Kategori ürünleri taþýndý: {kat.KategoriAdi} -> Diðer ({urunSayisi} ürün)");
                        
                        TempData["Success"] = $"{urunSayisi} ürün '{kat.KategoriAdi}' kategorisinden 'Diðer' kategorisine taþýndý.";
                    }
                    else
                    {
                        TempData["Error"] = "Bu kategoride taþýnacak ürün yok.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "? Ürünler taþýnýrken hata oluþtu: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Kategori bulunamadý.";
            }

            return RedirectToAction("Kategoriler");
        }

        [HttpPost]
        public async Task<IActionResult> KategoriSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var kat = await _context.Kategori
                .Include(k => k.Urun)
                .FirstOrDefaultAsync(k => k.KategoriId == id);

            if (kat != null)
            {
                // "Diðer" kategorisi sistem tarafýndan korunmaktadýr
                if (kat.KategoriAdi.ToLower() == "diðer" || kat.KategoriAdi.ToLower() == "diger")
                {
                    TempData["Error"] = "'Diðer' kategorisi sistem tarafýndan korunmaktadýr ve silinemez!";
                    return RedirectToAction("Kategoriler");
                }

                try
                {
                    int urunSayisi = kat.Urun.Count;

                    // "Diðer" kategorisini bul veya oluþtur
                    var digerKategori = await _context.Kategori
                        .FirstOrDefaultAsync(k => k.KategoriAdi.ToLower() == "diðer" || k.KategoriAdi.ToLower() == "diger");
                    
                    if (digerKategori == null)
                    {
                        // "Diðer" kategorisi yoksa oluþtur
                        digerKategori = new Kategori 
                        { 
                            KategoriAdi = "Diðer", 
                            SiraNo = 9999,  // En sonda görünsün
                            ResimUrl = null 
                        };
                        _context.Kategori.Add(digerKategori);
                        await _context.SaveChangesAsync();
                    }

                    // Kategorideki tüm ürünleri "Diðer" kategorisine taþý
                    if (kat.Urun.Any())
                    {
                        foreach (var urun in kat.Urun)
                        {
                            urun.KategoriId = digerKategori.KategoriId;
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Kategoriyi sil
                    _context.Kategori.Remove(kat);
                    await _context.SaveChangesAsync();

                    if (urunSayisi > 0)
                    {
                        TempData["Success"] = $"{kat.KategoriAdi} silindi. {urunSayisi} ürün 'Diðer' kategorisine taþýndý.";
                    }
                    else
                    {
                        TempData["Success"] = $"{kat.KategoriAdi} silindi.";
                    }

                    // Log kaydet
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.KategoriSilme, $"Kategori silindi: {kat.KategoriAdi} (ID: {id}, Taþýnan Ürün: {urunSayisi})");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Kategori silinirken hata oluþtu: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Kategori bulunamadý.";
            }

            return RedirectToAction("Kategoriler");
        }


        // --- ÜRÜNLER SAYFASI ---
        [HttpGet]
        public async Task<IActionResult> Urunler(int? katId, string arama, int? durum, string siralama, int sayfa = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            int sayfaBasinaKayit = 8; 

            var query = _context.Urun
                .Include(u => u.Kategori)
                .AsQueryable();

            // 1. Kategori Filtresi
            if (katId.HasValue) 
                query = query.Where(u => u.KategoriId == katId.Value);

            // 2. Arama Filtresi
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim().ToLower();
                query = query.Where(u => u.UrunAdi.ToLower().Contains(arama));
            }

            // 3. Durum Filtresi (Aktif/Pasif)
            if (durum.HasValue)
            {
                byte durumByte = (byte)durum.Value;
                query = query.Where(u => u.AktifMi == durumByte);
            }

            // 4. Sýralama
            query = (siralama?.ToLower()) switch
            {
                "kategori" => query.OrderBy(u => u.Kategori.SiraNo).ThenBy(u => u.UrunAdi),
                "fiyat-artan" => query.OrderBy(u => u.Fiyat).ThenBy(u => u.UrunAdi),
                "fiyat-azalan" => query.OrderByDescending(u => u.Fiyat).ThenBy(u => u.UrunAdi),
                "stok-artan" => query.OrderBy(u => u.Stok).ThenBy(u => u.UrunAdi),
                "stok-azalan" => query.OrderByDescending(u => u.Stok).ThenBy(u => u.UrunAdi),
                _ => query.OrderBy(u => u.UrunAdi) // Varsayýlan: Ýsme göre
            };

            // Toplam kayýt sayýsý
            var toplamKayit = await query.CountAsync();
            var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            // Sayfa doðrulama
            if (sayfa < 1) sayfa = 1;
            if (sayfa > toplamSayfa && toplamSayfa > 0) sayfa = toplamSayfa;

            var urunler = await query
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .ToListAsync();

            // Dropdown için kategorileri gönderir
            ViewBag.Kategoriler = await _context.Kategori.OrderBy(k => k.SiraNo).ToListAsync();

            // Filtreleri geri yükle
            ViewBag.AktifKategori = katId;
            ViewBag.Arama = arama;
            ViewBag.AktifDurum = durum;
            ViewBag.Siralama = siralama ?? "ad";

            // Sayfalama bilgileri
            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.ToplamKayit = toplamKayit;

            return View(urunler);
        }

        [HttpPost]
        public async Task<IActionResult> UrunEkle(string ad, decimal fiyat, int stok, int katId, string url)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            // ÜRÜN ADI KONTROLÜ: URL yazýlmýþ mý?
            if (!string.IsNullOrWhiteSpace(ad) && (ad.StartsWith("http://") || ad.StartsWith("https://") || ad.StartsWith("www.")))
            {
                TempData["Error"] = "Ürün adý alanýna resim URL'si yazýlamaz! Lütfen ürün adýný doðru alana girin.";
                return RedirectToAction("Urunler");
            }
            
            // Deðer kontrolü
            if (fiyat < 0 || fiyat > 999999.99m)
            {
                TempData["Error"] = "Fiyat 0 ile 999.999,99 TL arasýnda olmalýdýr.";
                return RedirectToAction("Urunler");
            }
            
            if (stok < 0 || stok > 999999)
            {
                TempData["Error"] = "Stok 0 ile 999.999 arasýnda olmalýdýr.";
                return RedirectToAction("Urunler");
            }
            
            try
            {
                var yeniUrun = new Urun
                {
                    UrunAdi = ad,
                    Fiyat = fiyat,
                    Stok = stok,
                    KategoriId = katId,
                    ResimUrl = url,
                    AktifMi = 1 // Varsayýlan Aktif
                };
                _context.Urun.Add(yeniUrun);
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.UrunEkleme, $"Yeni ürün eklendi: {ad} (Fiyat: {fiyat:C}, Stok: {stok})");

                TempData["Success"] = "Ürün eklendi.";
            }
            catch 
            { 
                TempData["Error"] = "Ürün eklenirken hata oluþtu."; 
            }
            return RedirectToAction("Urunler");
        }

        [HttpPost]
        public async Task<IActionResult> UrunGuncelle(int id, string ad, decimal fiyat, int stok, int katId, string url, byte aktifMi)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            // ÜRÜN ADI KONTROLÜ: URL yazýlmýþ mý?
            if (!string.IsNullOrWhiteSpace(ad) && (ad.StartsWith("http://") || ad.StartsWith("https://") || ad.StartsWith("www.")))
            {
                TempData["Error"] = "Ürün adý alanýna resim URL'si yazýlamaz! Lütfen ürün adýný doðru alana girin.";
                return RedirectToAction("Urunler");
            }
            
            // Deðer kontrolü
            if (fiyat < 0 || fiyat > 999999.99m)
            {
                TempData["Error"] = "Fiyat 0 ile 999.999,99 TL arasýnda olmalýdýr.";
                return RedirectToAction("Urunler");
            }
            
            if (stok < 0 || stok > 999999)
            {
                TempData["Error"] = "Stok 0 ile 999.999 arasýnda olmalýdýr.";
                return RedirectToAction("Urunler");
            }
            
            var urun = await _context.Urun.FindAsync(id);
            if (urun != null)
            {
                urun.UrunAdi = ad;
                urun.Fiyat = fiyat;
                urun.Stok = stok;
                urun.KategoriId = katId;
                urun.ResimUrl = url;
                urun.AktifMi = aktifMi;

                await _context.SaveChangesAsync();

                // Log kaydet (AktifMi: 1 -> aktif, 0 -> pasif)
                var aktiflikText = aktifMi == 1 ? "aktif" : "pasif";
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.UrunGuncelleme, $"Ürün güncellendi: {ad} (ID: {id}, Fiyat: {fiyat:C}, Stok: {stok}, Durum: {aktiflikText})");

                TempData["Success"] = "Ürün güncellendi.";
            }
            else
            {
                TempData["Error"] = "Ürün bulunamadý.";
            }
            return RedirectToAction("Urunler");
        }

        [HttpPost]
        public async Task<IActionResult> UrunSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var urun = await _context.Urun.FindAsync(id);
            if (urun == null)
            {
                TempData["Error"] = "Ürün bulunamadý.";
                return RedirectToAction("Urunler");
            }

            string urunAdi = urun.UrunAdi;

            try
            {
                // 1. Ürün geçmiþ sipariþlerde kullanýlmýþ mý kontrol et
                var siparislerdeKullanildiMi = await _context.SiparisUrun
                    .AnyAsync(su => su.UrunId == id);

                if (siparislerdeKullanildiMi)
                {
                    // GEÇMÝÞ SÝPARÝÞLERDE VAR › Pasife al (kategorisini deðiþtirme)
                    urun.AktifMi = 0; // Pasif yap 
                    urun.Stok = 0; // Stoku sýfýrla
                   
                    
                    await _context.SaveChangesAsync();

                    // Log kaydet
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.UrunSilme, $"Ürün pasife alýndý: {urunAdi} (ID: {id})");

                    TempData["Success"] = $"{urunAdi} pasife alýndý.";
                }
                else
                {
                    // GEÇMÝÞTE HÝÇ KULLANILMAMIÞ › Fiziksel olarak sil
                    _context.Urun.Remove(urun);
                    await _context.SaveChangesAsync();

                    // Log kaydet
                    await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.UrunSilme, $"Ürün silindi: {urunAdi} (ID: {id})");

                    TempData["Success"] = $"{urunAdi} baþarýyla silindi.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ürün silinirken hata oluþtu: {ex.Message}";
            }
            
            return RedirectToAction("Urunler");
        }


        // ============================================================
        // 6. MASA ÝÞLEMLERÝ
        // ============================================================

        // A. MASALARI LÝSTELE
        [HttpGet]
        public async Task<IActionResult> Masalar()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            // Masalarý, Durumlarýný ve (Varsa) Aktif Sipariþlerini Çek
            // Not: Siparis.Where(...) ile sadece aktif (kapanmamýþ/iptal olmamýþ) sipariþleri almaya çalýþýyoruz.
            var masalar = await _context.Masa
                .Include(m => m.Durum)
                .Include(m => m.Siparis)
                .OrderBy(m => m.MasaAdi)
                .ToListAsync();

            // Modal içindeki dropdown için Masa Durumlarýný gönder
            ViewBag.MasaDurumlari = await _context.MasaDurum.ToListAsync();

            return View(masalar);
        }

        // B. YENÝ MASA EKLE
        [HttpPost]
        public async Task<IActionResult> MasaEkle(string masaAdi)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            try
            {
                var yeniMasa = new Masa
                {
                    MasaAdi = masaAdi,
                    DurumId = 1 // Varsayýlan: Boþ
                };
                _context.Masa.Add(yeniMasa);
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.MasaEkleme, $"Yeni masa eklendi: {masaAdi}");

                TempData["Success"] = "Masa eklendi.";
            }
            catch
            {
                TempData["Error"] = "Masa eklenirken hata oluþtu.";
            }

            return RedirectToAction("Masalar");
        }

        // C. MASA GÜNCELLE (Adý veya Durumu Deðiþtir)
        [HttpPost]
        public async Task<IActionResult> MasaGuncelle(int masaId, string masaAdi, int durumId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var masa = await _context.Masa.FindAsync(masaId);
            if (masa != null)
            {
                masa.MasaAdi = masaAdi;
                masa.DurumId = durumId;
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.MasaGuncelleme, $"Masa güncellendi: {masaAdi} (ID: {masaId})");

                TempData["Success"] = "Masa güncellendi.";
            }
            else
            {
                TempData["Error"] = "Masa bulunamadý.";
            }

            return RedirectToAction("Masalar");
        }

        // D. MASA SÝL
        [HttpPost]
        public async Task<IActionResult> MasaSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var masa = await _context.Masa.FindAsync(id);
            if (masa != null)
            {
                // 1. Masa durumu kontrolü (Dolu veya Rezerve masalar silinemez)
                if (masa.DurumId == 2) // Dolu
                {
                    TempData["Error"] = "Dolu masa silinemez! Önce sipariþi kapatýn.";
                    return RedirectToAction("Masalar");
                }

                if (masa.DurumId == 3) // Rezerve
                {
                    TempData["Error"] = "Rezerve masa silinemez! Önce rezervasyonu iptal edin.";
                    return RedirectToAction("Masalar");
                }

                // 2. Aktif sipariþ kontrolü (Beklemede veya Hazýrlanýyor)
                var aktifSiparisVarMi = await _context.Siparis
                    .AnyAsync(s => s.MasaId == id && (s.SiparisDurumId == 1 || s.SiparisDurumId == 2));

                if (aktifSiparisVarMi)
                {
                    TempData["Error"] = "Bu masada açýk sipariþ var, silinemez!";
                    return RedirectToAction("Masalar");
                }

                _context.Masa.Remove(masa);
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.MasaSilme, $"Masa silindi: {masa.MasaAdi} (ID: {id})");

                TempData["Success"] = $"{masa.MasaAdi} silindi.";
            }
            else
            {
                TempData["Error"] = "Masa bulunamadý.";
            }

            return RedirectToAction("Masalar");
        }


        // ============================================================
        // 7. GÝDER (MASRAF) YÖNETÝMÝ
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Giderler(DateTime? baslangic, DateTime? bitis, int? katId, string arama, string siralama)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            // KURAL: Bitiþ tarihi seçilmediyse, varsayýlan olarak BUGÜN olsun
            if (!bitis.HasValue)
            {
                bitis = DateTime.Today;
            }

            var query = _context.Gider
                .Include(g => g.GiderKategori)
                .Include(g => g.Personel)
                .AsQueryable();

            // 1. Baþlangýç Filtresi (Varsa uygula, yoksa tüm geçmiþi getir)
            if (baslangic.HasValue)
            {
                query = query.Where(g => g.GiderTarihi >= baslangic.Value);
            }

            // 2. Bitiþ Filtresi (Seçilen günün son saniyesine kadar)
            var bitisSonu = bitis.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(g => g.GiderTarihi <= bitisSonu);

            // 3. Kategori Filtresi
            if (katId.HasValue)
            {
                query = query.Where(g => g.GiderKategoriId == katId.Value);
            }

            // 4. Arama Filtresi (Açýklama, Personel Adý, Kategori, Tutar)
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim().ToLower();

                // Tutar için sayýya çevirmeyi dene
                bool aramaNumeric = decimal.TryParse(arama, out decimal tutarArama);

                query = query.Where(g =>
                    // Açýklama
                    (g.Aciklama != null && g.Aciklama.ToLower().Contains(arama)) ||
                    // Personel Adý
                    (g.Personel != null && g.Personel.AdSoyad.ToLower().Contains(arama)) ||
                    // Kategori Adý
                    (g.GiderKategori != null && g.GiderKategori.KategoriAdi.ToLower().Contains(arama)) ||
                    // Tutar
                    (aramaNumeric && g.Tutar == tutarArama)
                );
            }

            // 5. Sýralama
            query = (siralama?.ToLower()) switch
            {
                "tarih-eski" => query.OrderBy(g => g.GiderTarihi),
                "tutar-artan" => query.OrderBy(g => g.Tutar).ThenByDescending(g => g.GiderTarihi),
                "tutar-azalan" => query.OrderByDescending(g => g.Tutar).ThenByDescending(g => g.GiderTarihi),
                "kategori" => query.OrderBy(g => g.GiderKategori.KategoriAdi).ThenByDescending(g => g.GiderTarihi),
                _ => query.OrderByDescending(g => g.GiderTarihi) // Varsayýlan: Tarihe göre (yeni›eski)
            };

            var giderler = await query.ToListAsync();

            // ViewBag'e gönder (kategori listesini alfabetik / artan sýrada göster)
            ViewBag.Kategoriler = await _context.GiderKategori
                .OrderBy(k => k.GiderKategoriId)
                .ToListAsync();
            ViewBag.Baslangic = baslangic;
            ViewBag.Bitis = bitis;
            ViewBag.AktifKat = katId;
            ViewBag.Arama = arama;
            ViewBag.Siralama = siralama ?? "tarih";

            return View(giderler);
        }

        [HttpPost]
        public async Task<IActionResult> GiderEkle(string aciklama, decimal tutar, int katId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Deðer kontrolü
            if (tutar < 0 || tutar > 999999.99m)
            {
                TempData["Error"] = "Gider tutarý 0 ile 999.999,99 TL arasýnda olmalýdýr.";
                return RedirectToAction("Giderler");
            }

            try
            {
                // Gideri kaydeden personeli Session'dan alýyoruz
                int? personelId = HttpContext.Session.GetInt32("PersonelId");

                var yeniGider = new Gider
                {
                    Aciklama = aciklama,
                    Tutar = tutar,
                    GiderKategoriId = katId,
                    PersonelId = personelId,
                    GiderTarihi = DateTime.Now
                };

                _context.Gider.Add(yeniGider);
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.GiderEkleme, $"Yeni gider eklendi: {aciklama} ({tutar:C})");

                TempData["Success"] = "Gider kaydedildi.";
            }
            catch
            {
                TempData["Error"] = "Gider eklenirken hata oluþtu.";
            }

            return RedirectToAction("Giderler");
        }

        [HttpPost]
        public async Task<IActionResult> GiderGuncelle(int id, string aciklama, decimal tutar, int katId, DateTime tarih)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Deðer kontrolü
            if (tutar < 0 || tutar > 999999.99m)
            {
                TempData["Error"] = "Gider tutarý 0 ile 999.999,99 TL arasýnda olmalýdýr.";
                return RedirectToAction("Giderler");
            }

            var gider = await _context.Gider.FindAsync(id);
            if (gider != null)
            {
                gider.Aciklama = aciklama;
                gider.Tutar = tutar;
                gider.GiderKategoriId = katId;
                gider.GiderTarihi = tarih; // Tarihi de düzeltebilsin

                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.GiderGuncelleme, $"Gider güncellendi: {aciklama} (ID: {id}, Tutar: {tutar:C})");

                TempData["Success"] = "Gider güncellendi.";
            }
            else
            {
                TempData["Error"] = "Gider bulunamadý.";
            }
            return RedirectToAction("Giderler");
        }

        [HttpPost]
        public async Task<IActionResult> GiderSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var gider = await _context.Gider.FindAsync(id);
            if (gider != null)
            {
                var aciklama = gider.Aciklama;
                var tutar = gider.Tutar;

            _context.Gider.Remove(gider);
            await _context.SaveChangesAsync();

            // Log kaydet
            await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.GiderSilme, $"Gider silindi: {aciklama} (ID: {id}, Tutar: {tutar:C})");

            TempData["Success"] = "Gider silindi.";
            }
            return RedirectToAction("Giderler");
        }

        // ============================================================
        // 8. RAPORLAR VE ÝSTATÝSTÝKLER
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Rapor(DateTime? baslangic, DateTime? bitis)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            // Varsayýlan: Son 30 Gün
            if (!baslangic.HasValue) baslangic = DateTime.Today.AddDays(-30);
            if (!bitis.HasValue) bitis = DateTime.Today.AddDays(1).AddTicks(-1);

            // 1. ÖZET VERÝLER
            var toplamCiro = await _context.Odeme
                .Where(o => o.Tarih >= baslangic && o.Tarih <= bitis)
                .SumAsync(o => (decimal?)o.Tutar) ?? 0;

            var toplamGider = await _context.Gider
                .Where(g => g.GiderTarihi >= baslangic && g.GiderTarihi <= bitis)
                .SumAsync(g => (decimal?)g.Tutar) ?? 0;

            var netKar = toplamCiro - toplamGider;

            var siparisSayisi = await _context.Siparis
                .CountAsync(s => s.OlusturmaTarihi >= baslangic && s.OlusturmaTarihi <= bitis && s.SiparisDurumId != 4);

            // 2. GRAFÝK 1: EN ÇOK SATAN 10 ÜRÜN
            var populerUrunler = await _context.SiparisUrun
                .Include(su => su.Siparis)
                .Include(su => su.Urun)
                .Where(su => su.Siparis.OlusturmaTarihi >= baslangic && su.Siparis.OlusturmaTarihi <= bitis && su.Siparis.SiparisDurumId != 4)
                .GroupBy(su => su.Urun.UrunAdi)
                .Select(g => new { UrunAdi = g.Key, Adet = g.Sum(x => x.Adet) })
                .OrderByDescending(x => x.Adet)
                .Take(10)
                .ToListAsync();

            // 3. GRAFÝK 2: GÝDER DAÐILIMI
            var giderDagilimi = await _context.Gider
                .Include(g => g.GiderKategori)
                .Where(g => g.GiderTarihi >= baslangic && g.GiderTarihi <= bitis)
                .GroupBy(g => g.GiderKategori.KategoriAdi)
                .Select(g => new { Kategori = g.Key, Tutar = g.Sum(x => x.Tutar) })
                .ToListAsync();

            // 4. EN BAÞARILI PERSONELLER (Garsonlar - Sipariþ Sayýsýna Göre)
            var personelPerformansi = await _context.Siparis
                .Include(s => s.Garson)
                .Include(s => s.SiparisUrun)
                .Where(s => s.OlusturmaTarihi >= baslangic && s.OlusturmaTarihi <= bitis && s.SiparisDurumId != 4 && s.GarsonId != null)
                .ToListAsync();

            var personelGruplu = personelPerformansi
                .GroupBy(s => new { s.Garson.PersonelId, s.Garson.AdSoyad })
                .Select(g => new
                {
                    PersonelAdi = g.Key.AdSoyad,
                    SiparisSayisi = g.Count(),
                    ToplamCiro = g.Sum(s => s.SiparisUrun.Sum(su => su.Adet * su.BirimFiyat))
                })
                .OrderByDescending(x => x.ToplamCiro)
                .Take(5)
                .ToList();

            // 5. KATEGORÝ BAZLI SATIÞ ANALÝZÝ
            var kategoriSatislari = await _context.SiparisUrun
                .Include(su => su.Siparis)
                .Include(su => su.Urun)
                .ThenInclude(u => u.Kategori)
                .Where(su => su.Siparis.OlusturmaTarihi >= baslangic && su.Siparis.OlusturmaTarihi <= bitis && su.Siparis.SiparisDurumId != 4)
                .GroupBy(su => su.Urun.Kategori.KategoriAdi)
                .Select(g => new
                {
                    Kategori = g.Key,
                    ToplamSatis = g.Sum(x => x.Adet * x.BirimFiyat),
                    UrunSayisi = g.Sum(x => x.Adet)
                })
                .OrderByDescending(x => x.ToplamSatis)
                .ToListAsync();

            // 6. GÜNLÜK CÝRO TREND (Son 7 Gün veya Seçilen Dönemin Son 7 Günü)
            var gunlukCiroBaslangic = bitis.Value.AddDays(-6).Date;
            var gunlukCiro = await _context.Odeme
                .Where(o => o.Tarih >= gunlukCiroBaslangic && o.Tarih <= bitis)
                .GroupBy(o => o.Tarih.Date)
                .Select(g => new
                {
                    Tarih = g.Key,
                    Tutar = g.Sum(x => x.Tutar)
                })
                .OrderBy(x => x.Tarih)
                .ToListAsync();

            // Eksik günleri 0 ile doldur
            var tumGunler = new List<dynamic>();
            for (int i = 0; i < 7; i++)
            {
                var gun = gunlukCiroBaslangic.AddDays(i);
                var mevcutCiro = gunlukCiro.FirstOrDefault(g => g.Tarih.Date == gun);
                tumGunler.Add(new
                {
                    Tarih = gun.ToString("dd MMM"),
                    Tutar = mevcutCiro?.Tutar ?? 0
                });
            }

            // 7. ÝPTAL EDÝLEN SÝPARÝÞ ANALÝZÝ
            var iptalEdilenSiparisSayisi = await _context.Siparis
                .CountAsync(s => s.OlusturmaTarihi >= baslangic && s.OlusturmaTarihi <= bitis && s.SiparisDurumId == 4);

            var iptalOrani = siparisSayisi > 0 ? ((double)iptalEdilenSiparisSayisi / (siparisSayisi + iptalEdilenSiparisSayisi)) * 100 : 0;

            // 8. ORTALAMA SÝPARÝÞ DEÐERÝ
            var ortalamaSiparisDegeri = siparisSayisi > 0 ? toplamCiro / siparisSayisi : 0;

            // 9. EN YÜKSEK CÝRO YAPILAN GÜN
            var enYuksekGun = await _context.Odeme
                .Where(o => o.Tarih >= baslangic && o.Tarih <= bitis)
                .GroupBy(o => o.Tarih.Date)
                .Select(g => new { Tarih = g.Key, Tutar = g.Sum(x => x.Tutar) })
                .OrderByDescending(x => x.Tutar)
                .FirstOrDefaultAsync();

            // 10. ÖDEME YÖNTEMÝ ÝSTATÝSTÝKLERÝ
            var odemeIstatistikleri = await _context.Odeme
                .Include(o => o.OdemeTuru)
                .Where(o => o.Tarih >= baslangic && o.Tarih <= bitis)
                .GroupBy(o => new { o.OdemeTuruId, o.OdemeTuru.OdemeTuruAdi })
                .Select(g => new 
                { 
                    OdemeTuruId = g.Key.OdemeTuruId,
                    OdemeTuruAdi = g.Key.OdemeTuruAdi,
                    Adet = g.Count(),
                    Tutar = g.Sum(x => x.Tutar)
                })
                .OrderByDescending(x => x.Adet)
                .ToListAsync();

            var toplamOdemeSayisi = odemeIstatistikleri.Sum(o => o.Adet);

            // 11. VERÝLERÝ VÝEW'A TAÞIMA
            ViewBag.Baslangic = baslangic.Value.ToString("yyyy-MM-dd");
            ViewBag.Bitis = bitis.Value.ToString("yyyy-MM-dd");

            ViewBag.ToplamCiro = toplamCiro;
            ViewBag.ToplamGider = toplamGider;
            ViewBag.NetKar = netKar;
            ViewBag.SiparisSayisi = siparisSayisi;
            ViewBag.OrtalamaSiparisDegeri = ortalamaSiparisDegeri;
            ViewBag.IptalEdilenSiparisSayisi = iptalEdilenSiparisSayisi;
            ViewBag.IptalOrani = iptalOrani;
            ViewBag.EnYuksekGun = enYuksekGun;

            // Ödeme Yöntemi Ýstatistikleri
            ViewBag.OdemeIstatistikleri = odemeIstatistikleri;
            ViewBag.ToplamOdemeSayisi = toplamOdemeSayisi;

            // Bugünün ve Haftanýn Satýþ Sayýlarý
            var bugun = DateTime.Today;
            var haftaBaslangic = bugun.AddDays(-7);
            
            var bugununSatisSayisi = await _context.Siparis
                .CountAsync(s => s.OlusturmaTarihi.Date == bugun && s.SiparisDurumId != 4);
            
            var haftaninSatisSayisi = await _context.Siparis
                .CountAsync(s => s.OlusturmaTarihi >= haftaBaslangic && s.OlusturmaTarihi <= bugun && s.SiparisDurumId != 4);
            
            ViewBag.BugununSatisSayisi = bugununSatisSayisi;
            ViewBag.HaftaninSatisSayisi = haftaninSatisSayisi;

            // ============================================================
            // AI TAHMÝN VERÝLERÝ - Artýk AJAX ile yüklenecek
            // ============================================================
            // AI verileri sayfa yüklendikten sonra JavaScript ile çekilecek
            ViewBag.AiServisAktif = false; // Baþlangýçta false, AJAX ile güncellenecek

            // JSON Verileri - Grafikler için
            ViewBag.UrunIsimleri = Newtonsoft.Json.JsonConvert.SerializeObject(populerUrunler.Select(x => x.UrunAdi));
            ViewBag.UrunAdetleri = Newtonsoft.Json.JsonConvert.SerializeObject(populerUrunler.Select(x => x.Adet));

            ViewBag.GiderKategorileri = Newtonsoft.Json.JsonConvert.SerializeObject(giderDagilimi.Select(x => x.Kategori));
            ViewBag.GiderTutarlari = Newtonsoft.Json.JsonConvert.SerializeObject(giderDagilimi.Select(x => x.Tutar));

            ViewBag.PersonelIsimleri = Newtonsoft.Json.JsonConvert.SerializeObject(personelGruplu.Select(x => x.PersonelAdi));
            ViewBag.PersonelCiro = Newtonsoft.Json.JsonConvert.SerializeObject(personelGruplu.Select(x => x.ToplamCiro));
            ViewBag.PersonelSiparisSayisi = Newtonsoft.Json.JsonConvert.SerializeObject(personelGruplu.Select(x => x.SiparisSayisi));

            ViewBag.KategoriIsimleri = Newtonsoft.Json.JsonConvert.SerializeObject(kategoriSatislari.Select(x => x.Kategori));
            ViewBag.KategoriSatislari = Newtonsoft.Json.JsonConvert.SerializeObject(kategoriSatislari.Select(x => x.ToplamSatis));

            ViewBag.GunlukTarihler = Newtonsoft.Json.JsonConvert.SerializeObject(tumGunler.Select(x => x.Tarih));
            ViewBag.GunlukCiroTutarlari = Newtonsoft.Json.JsonConvert.SerializeObject(tumGunler.Select(x => x.Tutar));

            return View();
        }

        // ============================================================
        // AI VERÝLERÝ JSON ENDPOINT (AJAX için)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAiData()
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                // Paralel çaðrý ile hýzlandýr
                var yarinTask = GetAiDataAsync<AiYarinTahmini>("/tahmin/yarin");
                var stokTask = GetAiDataAsync<AiStokTahmini>("/tahmin/stok");
                
                await Task.WhenAll(yarinTask, stokTask);
                
                var yarinTahmini = yarinTask.Result;
                var stokTahmini = stokTask.Result;

                // JavaScript'e snake_case formatýnda gönder
                return Json(new
                {
                    basari = true,
                    aiAktif = yarinTahmini != null || stokTahmini != null,
                    yarinTahmini = yarinTahmini == null ? null : new
                    {
                        basari = yarinTahmini.Basari,
                        tarih = yarinTahmini.Tarih,
                        gunAdi = yarinTahmini.GunAdi,
                        tahminiSatis = yarinTahmini.TahminiSatis,
                        tahminiCiro = yarinTahmini.TahminiCiro,
                        ortalamaSatis7gun = yarinTahmini.OrtalamaSatis7gun,
                        ortalamaCiro7gun = yarinTahmini.OrtalamaCiro7gun,
                        satisDegisimYuzde = yarinTahmini.SatisDegisimYuzde,
                        ciroDegisimYuzde = yarinTahmini.CiroDegisimYuzde
                    },
                    stokTahmini = stokTahmini == null ? null : new
                    {
                        basari = stokTahmini.Basari,
                        kritikUrunSayisi = stokTahmini.KritikUrunSayisi,
                        tukenenUrunSayisi = stokTahmini.TukenenUrunSayisi,
                        kritikUrunler = stokTahmini.KritikUrunler?.Select(u => new
                        {
                            urunAdi = u.UrunAdi,
                            mevcutStok = u.MevcutStok,
                            tahminiKalanGun = u.TahminiKalanGun,
                            aciliyet = u.Aciliyet
                        }).ToList()
                    }
                });
            }
            catch
            {
                return Json(new { basari = false, aiAktif = false });
            }
        }

        // PDF Rapor Ýndirme
        [HttpGet]
        public async Task<IActionResult> RaporPdfIndir(DateTime? baslangic, DateTime? bitis)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Varsayýlan: Son 30 Gün
            if (!baslangic.HasValue) baslangic = DateTime.Today.AddDays(-30);
            if (!bitis.HasValue) bitis = DateTime.Today.AddDays(1).AddTicks(-1);

            // Verileri topla
            var toplamCiro = await _context.Odeme
                .Where(o => o.Tarih >= baslangic && o.Tarih <= bitis)
                .SumAsync(o => (decimal?)o.Tutar) ?? 0;

            var toplamGider = await _context.Gider
                .Where(g => g.GiderTarihi >= baslangic && g.GiderTarihi <= bitis)
                .SumAsync(g => (decimal?)g.Tutar) ?? 0;

            var netKar = toplamCiro - toplamGider;

            var siparisSayisi = await _context.Siparis
                .CountAsync(s => s.OlusturmaTarihi >= baslangic && s.OlusturmaTarihi <= bitis && s.SiparisDurumId != 4);

            var ortalamaSiparisDegeri = siparisSayisi > 0 ? toplamCiro / siparisSayisi : 0;

            // En çok satan ürünler
            var enCokSatanlar = await _context.SiparisUrun
                .Include(su => su.Siparis)
                .Include(su => su.Urun)
                .Where(su => su.Siparis.OlusturmaTarihi >= baslangic && su.Siparis.OlusturmaTarihi <= bitis && su.Siparis.SiparisDurumId != 4)
                .GroupBy(su => su.Urun.UrunAdi)
                .Select(g => new { UrunAdi = g.Key, Adet = g.Sum(x => x.Adet) })
                .OrderByDescending(x => x.Adet)
                .Take(10)
                .ToListAsync();

            var enCokSatanlarList = enCokSatanlar.Select(u => (u.UrunAdi, u.Adet)).ToList();

            // Gider daðýlýmý
            var giderDagilimi = await _context.Gider
                .Include(g => g.GiderKategori)
                .Where(g => g.GiderTarihi >= baslangic && g.GiderTarihi <= bitis)
                .GroupBy(g => g.GiderKategori.KategoriAdi)
                .Select(g => new { Kategori = g.Key, Tutar = g.Sum(x => x.Tutar) })
                .ToListAsync();

            var giderDagilimiList = giderDagilimi.Select(g => (g.Kategori, g.Tutar)).ToList();

            // Personel performansý
            var personelPerformansi = await _context.Siparis
                .Include(s => s.Garson)
                .Include(s => s.SiparisUrun)
                .Where(s => s.OlusturmaTarihi >= baslangic && s.OlusturmaTarihi <= bitis && s.SiparisDurumId != 4 && s.GarsonId != null)
                .ToListAsync();

            var personelGruplu = personelPerformansi
                .GroupBy(s => new { s.Garson.PersonelId, s.Garson.AdSoyad })
                .Select(g => new
                {
                    PersonelAdi = g.Key.AdSoyad,
                    SiparisSayisi = g.Count(),
                    ToplamCiro = g.Sum(s => s.SiparisUrun.Sum(su => su.Adet * su.BirimFiyat))
                })
                .OrderByDescending(x => x.ToplamCiro)
                .Take(5)
                .ToList();

            var personelList = personelGruplu.Select(p => (p.PersonelAdi, p.ToplamCiro, p.SiparisSayisi)).ToList();

            // Kategori satýþlarý
            var kategoriSatislari = await _context.SiparisUrun
                .Include(su => su.Siparis)
                .Include(su => su.Urun)
                .ThenInclude(u => u.Kategori)
                .Where(su => su.Siparis.OlusturmaTarihi >= baslangic && su.Siparis.OlusturmaTarihi <= bitis && su.Siparis.SiparisDurumId != 4)
                .GroupBy(su => su.Urun.Kategori.KategoriAdi)
                .Select(g => new
                {
                    Kategori = g.Key,
                    ToplamSatis = g.Sum(x => x.Adet * x.BirimFiyat)
                })
                .OrderByDescending(x => x.ToplamSatis)
                .ToListAsync();

            var kategoriList = kategoriSatislari.Select(k => (k.Kategori, k.ToplamSatis)).ToList();

            // Ýptal oraný
            var iptalEdilenSiparisSayisi = await _context.Siparis
                .CountAsync(s => s.OlusturmaTarihi >= baslangic && s.OlusturmaTarihi <= bitis && s.SiparisDurumId == 4);

            var iptalOrani = siparisSayisi > 0 ? ((double)iptalEdilenSiparisSayisi / (siparisSayisi + iptalEdilenSiparisSayisi)) * 100 : 0;

            // En yüksek ciro günü
            var enYuksekGunData = await _context.Odeme
                .Where(o => o.Tarih >= baslangic && o.Tarih <= bitis)
                .GroupBy(o => o.Tarih.Date)
                .Select(g => new { Tarih = g.Key, Tutar = g.Sum(x => x.Tutar) })
                .OrderByDescending(x => x.Tutar)
                .FirstOrDefaultAsync();

            (DateTime Tarih, decimal Tutar)? enYuksekGun = enYuksekGunData != null 
                ? (enYuksekGunData.Tarih, enYuksekGunData.Tutar) 
                : null;

            // Ödeme yöntemleri istatistikleri
            var odemeIstatistikleri = await _context.Odeme
                .Include(o => o.OdemeTuru)
                .Where(o => o.Tarih >= baslangic && o.Tarih <= bitis)
                .GroupBy(o => o.OdemeTuru.OdemeTuruAdi)
                .Select(g => new 
                { 
                    OdemeTuruAdi = g.Key,
                    Adet = g.Count(),
                    Tutar = g.Sum(x => x.Tutar)
                })
                .OrderByDescending(x => x.Adet)
                .ToListAsync();

            var odemeYontemleriList = odemeIstatistikleri.Select(o => (o.OdemeTuruAdi, o.Adet, o.Tutar)).ToList();

            // Rapor türü belirle
            var gunFarki = (bitis.Value - baslangic.Value).Days;
            string raporTuru = gunFarki <= 1 ? "Günlük" : gunFarki <= 7 ? "Haftalýk" : gunFarki <= 31 ? "Aylýk" : "Dönemsel";

            // PDF Oluþtur
            byte[] pdfBytes = PdfRaporHelper.SatisRaporuOlustur(
                baslangic.Value,
                bitis.Value,
                toplamCiro,
                toplamGider,
                netKar,
                siparisSayisi,
                ortalamaSiparisDegeri,
                enCokSatanlarList,
                giderDagilimiList,
                personelList,
                kategoriList,
                iptalEdilenSiparisSayisi,
                iptalOrani,
                enYuksekGun,
                odemeYontemleriList,
                raporTuru
            );

            // Dosya adý
            string dosyaAdi = $"Satis_Raporu_{raporTuru}_{baslangic:yyyyMMdd}_{bitis:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", dosyaAdi);
        }

        // ============================================================
        // 9. ÝÞLEM LOGLARI
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Log(DateTime? baslangic, DateTime? bitis, int? turId, int? personelId, string arama, int sayfa = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            int sayfaBasinaKayit = 20; // Her sayfada 20 log

            var query = _context.IslemLog
                .Include(l => l.IslemTuru)
                .Include(l => l.Personel)
                .AsQueryable();

            // 1. Baþlangýç Filtresi (sadece belirtilmiþse uygula)
            if (baslangic.HasValue)
            {
                var baslangicTarihi = baslangic.Value.Date;
                query = query.Where(l => l.Zaman >= baslangicTarihi);
            }

            // 2. Bitiþ Filtresi (sadece belirtilmiþse gün sonunu dahil ederek uygula)
            if (bitis.HasValue)
            {
                var bitisSonu = bitis.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.Zaman <= bitisSonu);
            }

            // 3. Ýþlem Türü Filtresi
            if (turId.HasValue)
            {
                query = query.Where(l => l.IslemTuruId == turId.Value);
            }

            // 4. Personel Filtresi
            if (personelId.HasValue)
            {
                query = query.Where(l => l.PersonelId == personelId.Value);
            }

            // 5. Arama Filtresi (Açýklama, IP, Personel Adý, Ýþlem Türü)
            if (!string.IsNullOrWhiteSpace(arama))
            {
                var aramaLower = arama.Trim().ToLower();
                query = query.Where(l =>
                    (l.IslemAciklama != null && l.IslemAciklama.ToLower().Contains(aramaLower)) ||
                    (l.IpAdresi != null && l.IpAdresi.ToLower().Contains(aramaLower)) ||
                    (l.Personel != null && l.Personel.AdSoyad.ToLower().Contains(aramaLower)) ||
                    (l.IslemTuru != null && l.IslemTuru.TurAdi.ToLower().Contains(aramaLower))
                );
            }

            // Toplam kayýt sayýsý
            var toplamKayit = await query.CountAsync();
            var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            // Sayfa doðrulama
            if (sayfa < 1) sayfa = 1;
            if (sayfa > toplamSayfa && toplamSayfa > 0) sayfa = toplamSayfa;

            // Loglarý Zamana Göre Sýrala (Yeni›Eski) ve sayfala
            var loglar = await query
                .OrderByDescending(l => l.Zaman)
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .ToListAsync();

            // Dropdown'lar için veri gönder
            ViewBag.IslemTurleri = await _context.IslemTuru.ToListAsync();
            ViewBag.Personeller = await _context.Personel
                .Where(p => p.HesapDurumId == 1) // Sadece aktif personeller
                .OrderBy(p => p.AdSoyad)
                .ToListAsync();

            // Filtreleri geri yükle (null güvenli)
            ViewBag.Baslangic = baslangic;
            ViewBag.Bitis = bitis;
            ViewBag.AktifTur = turId;
            ViewBag.AktifPersonel = personelId;
            ViewBag.Arama = arama;

            // Sayfalama bilgileri
            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.ToplamKayit = toplamKayit;

            return View(loglar);
        }

        // ============================================================
        // 10. GERÝ BÝLDÝRÝMLER
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GeriBildirimler(DateTime? baslangic, DateTime? bitis, int? kategoriId, int? urunId, string siralama, string arama, int sayfa = 1, string turAdi = null)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Bildirimleri yükle
            await LoadNotifications();

            int sayfaBasinaKayit = 10; // Her sayfada 10 geri bildirim

            // KURAL: Bitiþ tarihi seçilmediyse, varsayýlan olarak BUGÜN olsun
            if (!bitis.HasValue)
            {
                bitis = DateTime.Today.AddDays(1).AddTicks(-1);
            }

            var query = _context.Geribildirim
                .Include(g => g.Urun)
                .ThenInclude(u => u.Kategori)
                .Include(g => g.Tur)
                .AsQueryable();

            // 1. Baþlangýç Filtresi
            if (baslangic.HasValue)
            {
                query = query.Where(g => g.Tarih >= baslangic.Value);
            }

            // 2. Bitiþ Filtresi (seçilen günün son saniyesine kadar dahil etmek için)
            if (bitis.HasValue)
            {
                var bitisSonu = bitis.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(g => g.Tarih <= bitisSonu);
            }

            // 3. Kategori Filtresi
            if (kategoriId.HasValue)
            {
                query = query.Where(g => g.Urun != null && g.Urun.KategoriId == kategoriId.Value);
            }

            // 4. Ürün Filtresi
            if (urunId.HasValue)
            {
                query = query.Where(g => g.UrunId == urunId.Value);
            }

            // 5. Arama Filtresi (Yorum, Ürün Adý)
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim().ToLower();
                query = query.Where(g =>
                    (g.Yorum != null && g.Yorum.ToLower().Contains(arama)) ||
                    (g.Urun != null && g.Urun.UrunAdi.ToLower().Contains(arama))
                );
            }

            // 6. Tür Filtresi (Ayrý dropdown)
            if (!string.IsNullOrWhiteSpace(turAdi))
            {
                query = query.Where(g => g.Tur != null && g.Tur.TurAdi == turAdi);
            }

            // 6. Sýralama
            query = (siralama?.ToLower()) switch
            {
                "tarih-eski" => query.OrderBy(g => g.Tarih),
                "puan-yuksek" => query.OrderByDescending(g => g.Puan).ThenByDescending(g => g.Tarih),
                "puan-dusuk" => query.OrderBy(g => g.Puan).ThenByDescending(g => g.Tarih),
                _ => query.OrderByDescending(g => g.Tarih) // Varsayýlan: Tarihe göre (Yeni›Eski)
            };

            // Toplam kayýt sayýsý
            var toplamKayit = await query.CountAsync();
            var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            // Sayfa doðrulama
            if (sayfa < 1) sayfa = 1;
            if (sayfa > toplamSayfa && toplamSayfa > 0) sayfa = toplamSayfa;

            // Geri bildirimleri getir
            var geriBildirimler = await query
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .ToListAsync();

            // Ýstatistikler: ayný filtreleri uygulayan sorguyu kullan (liste ile tutarlý olsun)
            var tumQuery = query; // query zaten include ve tüm filtreleri içeriyor (turAdi dahil)

            var ortalamaPuan = await tumQuery.AnyAsync() ? await tumQuery.AverageAsync(g => (double)g.Puan) : 0;
            var toplamYorum = await tumQuery.CountAsync();
            var puanDagilimi = await tumQuery
                .GroupBy(g => g.Puan)
                .Select(g => new { Puan = g.Key, Adet = g.Count() })
                .OrderByDescending(x => x.Puan)
                .ToListAsync();

            // En çok yorum alan ürünler
            var enCokYorumAlanUrunler = await tumQuery
                .Where(g => g.UrunId != null)
                .GroupBy(g => new { g.Urun.UrunId, g.Urun.UrunAdi })
                .Select(g => new 
                { 
                    UrunId = g.Key.UrunId, 
                    UrunAdi = g.Key.UrunAdi, 
                    YorumSayisi = g.Count(),
                    OrtalamaPuan = g.Average(x => (double)x.Puan)
                })
                .OrderByDescending(x => x.YorumSayisi)
                .Take(5)
                .ToListAsync();

            // Dropdown için kategoriler ve ürünler ve türler
            ViewBag.Kategoriler = await _context.Kategori
                .OrderBy(k => k.SiraNo)
                .ToListAsync();

            ViewBag.GeriBildirimTurleri = await _context.GeribildirimTuru
                .OrderBy(t => t.TurAdi)
                .ToListAsync();

            ViewBag.Urunler = await _context.Urun
                .Where(u => u.AktifMi == 1)
                .OrderBy(u => u.UrunAdi)
                .ToListAsync();

            // Filtreleri geri yükle
            ViewBag.Baslangic = baslangic;
            ViewBag.Bitis = bitis;
            ViewBag.AktifKategori = kategoriId;
            ViewBag.AktifUrun = urunId;
            ViewBag.Siralama = siralama ?? "tarih";
            ViewBag.Arama = arama;
            ViewBag.AktifTurAdi = turAdi;

            // Ýstatistikler
            ViewBag.OrtalamaPuan = ortalamaPuan;
            // Show total matching the main filtered list (toplamKayit) so view reflects the same filter results
            ViewBag.ToplamYorum = toplamKayit;
            ViewBag.PuanDagilimi = puanDagilimi;
            ViewBag.EnCokYorumAlanUrunler = enCokYorumAlanUrunler;

            // Sayfalama bilgileri
            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.ToplamKayit = toplamKayit;

            return View(geriBildirimler);
        }

        // Geri Bildirim Sil
        [HttpPost]
        public async Task<IActionResult> GeriBildirimSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var geriBildirim = await _context.Geribildirim.FindAsync(id);
            if (geriBildirim != null)
            {
                var urunId = geriBildirim.UrunId;

                _context.Geribildirim.Remove(geriBildirim);
                await _context.SaveChangesAsync();

                // Log kaydet
                await LogHelper.LogKaydetAsync(_context, HttpContext, IslemTurleri.GeriBildirimSilme, $"Geri bildirim silindi (ID: {id}, Ürün ID: {urunId})");

                TempData["Success"] = "Geri bildirim silindi.";
            }
            else
            {
                TempData["Error"] = "Geri bildirim bulunamadý.";
            }

            return RedirectToAction("GeriBildirimler");
        }
    }
}
