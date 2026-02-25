using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoranYonetim.Models;

namespace RestoranYonetim.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RestoranDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, RestoranDbContext context, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            // Kategorileri ve aktif ürünleri çek
            var kategoriler = await _context.Kategori
                .Include(k => k.Urun.Where(u => u.AktifMi == 1))
                .OrderBy(k => k.SiraNo)
                .ToListAsync();

            // Masa listesini ViewBag'e ekle (Garson Çaðýr için)
            ViewBag.Masalar = await _context.Masa
                .OrderBy(m => m.MasaAdi)
                .ToListAsync();

            // Geri bildirim türlerini ViewBag'e ekle (sadece Genel ve Hizmet)
            ViewBag.GeriBildirimTurleri = await _context.GeribildirimTuru
                .Where(t => t.TurAdi == "Genel" || t.TurAdi == "Hizmet")
                .OrderBy(t => t.TurAdi)
                .ToListAsync();

            return View(kategoriler);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Geri Bildirim Gönder (Ürün ve Genel)
        [HttpPost]
        public async Task<IActionResult> GeriBildirimGonder(int? urunId, int turId, byte puan, string yorum)
        {
            try
            {
                var geriBildirim = new Geribildirim
                {
                    UrunId = urunId, // Ürün geri bildiriminde dolu, genel geri bildiriminde null
                    TurId = turId, // Ürün geri bildiriminde turId null, genel geri bildiriminde turId dolu
                    Puan = puan,
                    Yorum = yorum,
                    Tarih = DateTime.Now
                };

                _context.Geribildirim.Add(geriBildirim);
                await _context.SaveChangesAsync();

                TempData["Success"] = urunId.HasValue ? "Ürün deðerlendirmeniz için teþekkür ederiz!" : "Geri bildiriminiz için teþekkür ederiz!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geri bildirim kaydedilirken hata oluþtu");
                TempData["Error"] = "Geri bildiriminiz kaydedilemedi. Lütfen tekrar deneyin.";
            }

            return RedirectToAction("Index");
        }

        // Garson Çaðýr
        [HttpPost]
        public async Task<IActionResult> GarsonCagir(int masaId, string? not)
        {
            try
            {
                var masa = await _context.Masa.FindAsync(masaId);
                if (masa == null)
                {
                    TempData["Error"] = "Masa bulunamadý!";
                    return RedirectToAction("Index");
                }

                // Session'da bildirim listesi oluþtur
                var bildirimler = HttpContext.Session.GetString("GarsonBildirimleri");
                var yeniBildirim = $"{masa.MasaId}|{masa.MasaAdi}|{DateTime.Now:HH:mm}|{not ?? "Yok"}";
                
                if (string.IsNullOrEmpty(bildirimler))
                {
                    HttpContext.Session.SetString("GarsonBildirimleri", yeniBildirim);
                }
                else
                {
                    HttpContext.Session.SetString("GarsonBildirimleri", bildirimler + ";" + yeniBildirim);
                }

                TempData["Success"] = $"{masa.MasaAdi} için garson çaðrýldý! En kýsa sürede yanýnýza gelecektir.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Garson çaðýrma sýrasýnda hata oluþtu");
                TempData["Error"] = "Bir hata oluþtu. Lütfen tekrar deneyin.";
            }

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
