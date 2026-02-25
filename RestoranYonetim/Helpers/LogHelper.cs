using Microsoft.AspNetCore.Http;
using RestoranYonetim.Models;

namespace RestoranYonetim.Helpers
{
    /// <summary>
    /// Ýþlem loglarýný veritabanýna kaydetmek için yardýmcý sýnýf
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Ýþlem logunu veritabanýna kaydeder
        /// </summary>
        /// <param name="context">Veritabaný context'i</param>
        /// <param name="httpContext">HTTP context (Session ve IP için)</param>
        /// <param name="islemTuruId">Ýþlem türü ID'si (1-18)</param>
        /// <param name="aciklama">Ýþlem açýklamasý</param>
        /// <param name="personelId">Personel ID (opsiyonel, belirtilmezse Session'dan alýnýr)</param>
        /// <returns></returns>
        public static async Task LogKaydetAsync(
            RestoranDbContext context,
            HttpContext httpContext,
            int islemTuruId,
            string aciklama,
            int? personelId = null)
        {
            try
            {
                var log = new IslemLog
                {
                    PersonelId = personelId ?? httpContext.Session.GetInt32("PersonelId"),
                    IslemTuruId = islemTuruId,
                    IpAdresi = httpContext.Connection.RemoteIpAddress?.ToString(),
                    Zaman = DateTime.Now,
                    IslemAciklama = aciklama
                };
                context.IslemLog.Add(log);
                await context.SaveChangesAsync();
            }
            catch
            {
                // Log hatasý ana iþlemi etkilemesin
            }
        }
    }

    /// <summary>
    /// Ýþlem türleri için sabit deðerler
    /// </summary>
    public static class IslemTurleri
    {
        public const int Giris = 1;
        public const int Cikis = 2;
        public const int PersonelEkleme = 3;
        public const int PersonelGuncelleme = 4;
        public const int PersonelSilme = 5;
        public const int KategoriEkleme = 6;
        public const int KategoriGuncelleme = 7;
        public const int KategoriSilme = 8;
        public const int UrunEkleme = 9;
        public const int UrunGuncelleme = 10;
        public const int UrunSilme = 11;
        public const int MasaEkleme = 12;
        public const int MasaGuncelleme = 13;
        public const int MasaSilme = 14;
        public const int GiderEkleme = 15;
        public const int GiderGuncelleme = 16;
        public const int GiderSilme = 17;
        public const int GeriBildirimSilme = 18;
        
        // Garson Ýþlemleri
        public const int SiparisOlusturma = 19;
        public const int SiparisUrunEkleme = 20;
        public const int SiparisUrunCikarma = 21;
        public const int SiparisDurumGuncelleme = 22;
        public const int SiparisIptal = 23;
        public const int OdemeAlma = 24;
        public const int MasaRezervasyon = 25;
    }
}
