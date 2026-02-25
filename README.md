# ??? Restoran Yönetim Sistemi

ASP.NET Core MVC ile geliþtirilmiþ kapsamlý bir restoran yönetim sistemidir. Masa takibi, sipariþ yönetimi, ödeme iþlemleri, stok kontrolü, personel yönetimi, gider takibi, raporlama ve AI destekli tahmin gibi birçok modülü barýndýrýr.

---

## ?? Özellikler

### ????? Garson Paneli
- Masa listesi ve durumlarý (Boþ / Dolu / Rezerve)
- Sipariþ oluþturma, ürün ekleme/çýkarma
- Stok kontrolü ile otomatik stok düþümü
- Sipariþ notu ekleme
- Sipariþ durumu güncelleme (Hazýrlanýyor ? Servis Edildi ? Tamamlandý)
- Ödeme alma (Nakit / Kredi Kartý)
- Sipariþ iptal etme ve stok iadesi
- Aktif sipariþleri görüntüleme
- Masa rezervasyonu yapma/kaldýrma
- Garson bildirim sistemi (Müþteri çaðrý)

### ??? Yönetici (Admin) Paneli
- **Dashboard:** Günlük ciro, sipariþ sayýsý, masa durumu, kritik stok uyarýlarý
- **Personel Yönetimi:** Ekleme, güncelleme, silme (soft delete), mesai takibi
- **Kategori Yönetimi:** Ekleme, güncelleme, silme, ürün taþýma
- **Ürün Yönetimi:** Ekleme, güncelleme, silme (soft delete), stok takibi
- **Masa Yönetimi:** Ekleme, güncelleme, silme
- **Sipariþ Geçmiþi:** Filtreleme, arama, sýralama, sayfalama
- **Gider Yönetimi:** Masraf takibi, kategorize etme
- **Raporlama:** Ciro, gider, net kâr, en çok satanlar, personel performansý, kategori analizi, ödeme yöntemleri
- **PDF Rapor:** Detaylý satýþ raporunu PDF olarak indirme
- **Ýþlem Loglarý:** Tüm sistem iþlemlerinin kayýt altýna alýnmasý
- **Geri Bildirimler:** Müþteri yorumlarý ve puanlarý yönetimi
- **AI Tahminleri:** Satýþ ve stok tükenme tahminleri (ayrý Python servisi ile)

### ?? Müþteri Menüsü (Anasayfa)
- Kategorilere göre dijital menü görüntüleme
- Garson çaðýrma (bildirim sistemi)
- Genel ve ürüne özel geri bildirim gönderme

---

## ??? Teknolojiler

| Teknoloji | Açýklama |
|-----------|----------|
| **ASP.NET Core 8 MVC** | Backend framework |
| **Entity Framework Core 8** | ORM / Veritabaný eriþimi |
| **SQL Server** | Veritabaný |
| **Tailwind CSS + DaisyUI** | Frontend tasarým |
| **Chart.js** | Grafik ve istatistikler |
| **iTextSharp** | PDF rapor oluþturma |
| **Font Awesome** | Ýkon kütüphanesi |
| **jQuery** | DOM manipülasyonu ve AJAX |
| **Session** | Oturum yönetimi |
| **SHA-256** | Þifre hashleme |

---

## ?? Proje Yapýsý

```
RestoranYonetim/
??? Controllers/
?   ??? AdminController.cs      # Yönetici paneli iþlemleri
?   ??? AuthController.cs       # Giriþ / Çýkýþ iþlemleri
?   ??? GarsonController.cs     # Garson paneli iþlemleri
?   ??? HomeController.cs       # Müþteri menüsü ve geri bildirim
??? Models/
?   ??? RestoranDbContext.cs     # EF Core DbContext
?   ??? Personel.cs, Masa.cs, Siparis.cs, Urun.cs ...  # Entity modelleri
?   ??? AiModels.cs             # AI servisi modelleri
??? Helpers/
?   ??? LogHelper.cs            # Ýþlem loglama
?   ??? Sifreleme.cs            # SHA-256 þifreleme
?   ??? PdfRaporHelper.cs       # PDF rapor oluþturma
?   ??? DecimalModelBinder.cs   # Türkçe decimal desteði
??? Views/
?   ??? Admin/                  # Yönetici sayfalarý
?   ??? Auth/                   # Giriþ sayfasý
?   ??? Garson/                 # Garson sayfalarý
?   ??? Home/                   # Müþteri sayfalarý
?   ??? Shared/                 # Layout dosyalarý
??? wwwroot/                    # Statik dosyalar (CSS, JS, img, fonts)
??? Program.cs                  # Uygulama baþlangýcý
??? appsettings.json            # Ayar dosyasý
```

---

## ?? Kurulum

### Gereksinimler
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB veya Express)

### Adýmlar

1. **Projeyi klonlayýn:**
   ```bash
   git clone https://github.com/KULLANICI_ADI/RestoranYonetim.git
   cd RestoranYonetim
   ```

2. **Baðlantý dizesini yapýlandýrýn:**
   
   `RestoranYonetim/appsettings.json` dosyasýndaki `DefaultConnection` deðerini kendi SQL Server bilgilerinize göre güncelleyin:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=SUNUCU_ADI;Database=restoran_db;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Veritabanýný oluþturun:**
   
   SQL Server üzerinde `restoran_db` veritabanýný oluþturun ve gerekli tablolarý migration veya SQL script ile kurun.

4. **Projeyi çalýþtýrýn:**
   ```bash
   cd RestoranYonetim
   dotnet run
   ```

5. **Tarayýcýda açýn:**
   ```
   https://localhost:5001
   ```

---

## ??? Veritabaný Tablolarý

| Tablo | Açýklama |
|-------|----------|
| `personel` | Personel bilgileri ve hesap durumlarý |
| `rol` | Kullanýcý rolleri (Garson, Yönetici) |
| `masa` | Masa bilgileri |
| `masa_durum` | Masa durumlarý (Boþ, Dolu, Rezerve) |
| `kategori` | Ürün kategorileri |
| `urun` | Ürün bilgileri, fiyat ve stok |
| `siparis` | Sipariþ kayýtlarý |
| `siparis_durum` | Sipariþ durumlarý (Hazýrlanýyor, Servis Edildi, Tamamlandý, Ýptal) |
| `siparis_urun` | Sipariþ-Ürün iliþki tablosu |
| `odeme` | Ödeme kayýtlarý |
| `odeme_turu` | Ödeme türleri (Nakit, Kredi Kartý) |
| `gider` | Gider/masraf kayýtlarý |
| `gider_kategori` | Gider kategorileri |
| `geribildirim` | Müþteri geri bildirimleri |
| `geribildirim_turu` | Geri bildirim türleri |
| `islem_log` | Sistem iþlem loglarý |
| `islem_turu` | Ýþlem türleri |
| `hesap_durum` | Hesap durumlarý (Aktif, Pasif vb.) |
| `mesai_durum` | Mesai durumlarý |

---

## ?? Kullanýcý Rolleri

| Rol ID | Rol | Eriþim |
|--------|-----|--------|
| 1 | Garson | Garson Paneli |
| 2 | Yönetici | Admin Paneli + Garson Paneli |

---

## ?? Lisans

Bu proje eðitim amaçlý geliþtirilmiþtir.

---
