using System.Security.Cryptography;
using System.Text;

namespace RestoranYonetim.Helpers
{
    public static class Sifreleme
    {
        // Metin olarak gelen şifreyi (örn: "1234") alır, SHA-256 formatına çevirir.
        public static string Sifrele(string veri)
        {
            // Eğer boş veri gelirse işlem yapma, boş dön
            if (string.IsNullOrEmpty(veri)) return "";

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // 1. Gelen veriyi byte dizisine çevirip hashliyoruz
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(veri));

                // 2. Hashlenen karmaşık byte'ları okunabilir string'e (Hex) çeviriyoruz
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // "x2" formatı küçük harfli hex üretir (a,b,c...)
                }

                return builder.ToString();
            }
        }
    }
}
