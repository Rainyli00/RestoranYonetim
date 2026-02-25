using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoranYonetim.Models;
using RestoranYonetim.Helpers;

namespace RestoranYonetim.Controllers
{
    public class AuthController : Controller
    {
        private readonly RestoranDbContext dbcontext;

        public AuthController(RestoranDbContext context)
        {
            dbcontext = context;
        }

        // 1. LOGIN SAYFASINI GÖSTER
        [HttpGet]
        public IActionResult Login()
        {
            // Zaten giriş yapmışsa, rolüne uygun sayfaya fırlat
            if (HttpContext.Session.GetInt32("PersonelId") != null)
            {
                int rolId = HttpContext.Session.GetInt32("RolId") ?? 0;
                return RedirectToRole(rolId);
            }
            return View();
        }

        // 2. GİRİŞ İŞLEMİ (POST)
        [HttpPost]
        public async Task<IActionResult> Login(string kullaniciAdi, string sifre)
        {
            string hashliSifre = Sifreleme.Sifrele(sifre);

            var personel = await dbcontext.Personel
                .Include(p => p.Rol)
                .FirstOrDefaultAsync(p => p.KullaniciAdi == kullaniciAdi
                                       && p.SifreHash == hashliSifre
                                       && p.HesapDurumId == 1); // Sadece Aktifler

            if (personel != null)
            {
                // Mesaiye giriş yap (MesaiDurumId = 2: Mesaide)
                personel.MesaiDurumId = 2;
                personel.SonGirisTarihi = DateTime.Now;
                await dbcontext.SaveChangesAsync();

                // Session Doldur
                HttpContext.Session.SetInt32("PersonelId", personel.PersonelId);
                HttpContext.Session.SetString("AdSoyad", personel.AdSoyad);

                // Rol ID'sini sakla
                HttpContext.Session.SetInt32("RolId", personel.RolId);

                // Rol Adını sakla (Layout'ta göstermek için)
                HttpContext.Session.SetString("RolAdi", personel.Rol.RolAdi);

                // Log kaydet
                await LogHelper.LogKaydetAsync(dbcontext, HttpContext, IslemTurleri.Giris, 
                    $"Sisteme giriş yapıldı: {personel.AdSoyad} ({personel.Rol.RolAdi})", personel.PersonelId);

                // Yönlendir
                return RedirectToRole(personel.RolId);
            }

            ViewBag.Hata = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        // 3. ÇIKIŞ
        public async Task<IActionResult> Logout(string reason = null)
        {
            // Mesaiden çıkış yap (MesaiDurumId = 1: Mesai Dışı)
            var personelId = HttpContext.Session.GetInt32("PersonelId");
            var adSoyad = HttpContext.Session.GetString("AdSoyad");

            if (personelId.HasValue)
            {
                // Rol navigasyonunu da yükleyerek RolAdi'ya erişim sırasında NullReference oluşmasını önlüyoruz
                var personel = await dbcontext.Personel
                    .Include(p => p.Rol)
                    .FirstOrDefaultAsync(p => p.PersonelId == personelId.Value);

                if (personel != null)
                {
                    personel.MesaiDurumId = 1;
                    await dbcontext.SaveChangesAsync();

                    // Rol bilgisi null ise güvenli bir şekilde yedek metin kullan
                    var rolAdi = personel.Rol?.RolAdi ?? "(Rol bilinmiyor)";

                    // Log kaydet
                    var logMesaj = reason == "timeout"
                        ? $"Oturum zaman aşımı nedeniyle çıkış yapıldı: {adSoyad} ({rolAdi})"
                        : $"Sistemden çıkış yapıldı: {adSoyad} ({rolAdi})";
                    await LogHelper.LogKaydetAsync(dbcontext, HttpContext, IslemTurleri.Cikis, logMesaj, personelId.Value);
                }
            }

            HttpContext.Session.Clear();
            
            // Zaman aşımı ise uyarı göster
            if (reason == "timeout")
            {
                TempData["Warning"] = "Uzun süre işlem yapmadığınız için oturumunuz sonlandırıldı.";
            }
            
            return RedirectToAction("Login");
        }

        // --- DÜZELTİLMİŞ YÖNLENDİRME METODU ---
        private IActionResult RedirectToRole(int rolID)
        {
            // SENİN DB AYARINA GÖRE:

            if (rolID == 2) // Yönetici
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (rolID == 1) // Garson (ID'si 1 varsayıyorum)
            {
                return RedirectToAction("Index", "Garson");
            }

            // Eğer rolü tanımlı değilse (Hata durumu) tekrar Logine at
            return RedirectToAction("Login", "Account");

        }
    }
}