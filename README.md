# 🍽️ Restoran Yönetim Sistemi

Restoran işletmeleri için geliştirilmiş kapsamlı bir **web tabanlı yönetim sistemi**. Sipariş takibi, masa yönetimi, personel kontrolü, stok takibi, gider yönetimi, raporlama ve müşteri geri bildirimi gibi bir restoranın tüm operasyonel süreçlerini dijital ortamda yönetmenizi sağlar.

## 📸 Genel Bakış

| Panel | Açıklama |
|-------|----------|
| **Müşteri Menüsü** | Dijital menü görüntüleme, garson çağırma, geri bildirim gönderme |
| **Garson Paneli** | Masa durumları, sipariş oluşturma/düzenleme, ödeme alma |
| **Yönetici (Admin) Paneli** | Dashboard, personel/ürün/masa/gider yönetimi, raporlar, loglar |

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|--------|-----------|
| **Backend** | ASP.NET Core MVC (.NET 8) |
| **Veritabanı** | Microsoft SQL Server (MSSQL) |
| **ORM** | Entity Framework Core 8 |
| **Frontend** | Razor Views, Bootstrap, JavaScript |
| **Kimlik Doğrulama** | Session tabanlı oturum yönetimi (SHA-256 şifreleme) |
| **PDF Rapor** | iTextSharp |
| **AI Entegrasyonu** | Opsiyonel Python FastAPI servisi (satış tahmini, stok analizi) |

## ✨ Özellikler

### 🧑‍🍳 Müşteri Paneli (Ana Sayfa)
- Kategorilere göre dijital menü görüntüleme
- Ürün detayları ve fiyat bilgisi
- Garson çağırma (masa seçerek)
- Ürün ve genel hizmet geri bildirimi gönderme (1-5 puan + yorum)

### 👨‍🍳 Garson Paneli
- **Masa Görünümü** — Tüm masaların anlık durumu (Boş / Dolu / Rezerve)
- **Sipariş Yönetimi** — Ürün ekleme (+1), çıkarma (-1), not ekleme
- **Stok Kontrolü** — Otomatik stok düşme ve stok tükenme uyarısı
- **Sipariş Durumu** — Hazırlanıyor → Servis Edildi → Tamamlandı
- **Ödeme Alma** — Nakit / Kredi Kartı ile ödeme ve hesap kapatma
- **Sipariş İptali** — Stok iadesi ile otomatik sipariş iptal
- **Masa Rezervasyonu** — Masa rezerve etme / kaldırma
- **Aktif Siparişler** — Tüm garsonların açık siparişlerini görüntüleme
- **Bildirim Sistemi** — Müşteri garson çağrılarını anlık bildirim olarak alma

### 🏢 Yönetici (Admin) Paneli
- **Dashboard (Kokpit)** — Günlük ciro, sipariş sayısı, masa durumu, kritik stok uyarıları
- **Personel Yönetimi** — Ekleme, düzenleme, silme (pasife alma), rol atama, hesap durumu, mesai takibi
- **Kategori Yönetimi** — Menü kategorileri oluşturma, sıralama, silme (ürünleri "Diğer"e taşıma)
- **Ürün Yönetimi** — Ürün CRUD, fiyat/stok güncelleme, aktif/pasif yönetimi
- **Masa Yönetimi** — Masa ekleme/düzenleme/silme, durum kontrolü
- **Sipariş Geçmişi** — Tarih, durum, tutar filtreli arama ve sıralama
- **Gider Yönetimi** — Gider kategorileri (kira, fatura, maaş vb.), kayıt ve raporlama
- **Raporlar & İstatistikler**
  - Toplam ciro, gider, net kâr hesaplama
  - En çok satan ürünler (Top 10)
  - Kategori bazlı satış analizi
  - Gider dağılımı grafiği
  - Personel performansı (ciro ve sipariş bazlı)
  - Günlük ciro trendi
  - Ödeme yöntemi istatistikleri
  - İptal oranı analizi
  - **PDF rapor indirme** (Günlük / Haftalık / Aylık / Dönemsel)
- **Geri Bildirimler** — Müşteri puanları, yorum analizi, ürün bazlı istatistikler
- **İşlem Logları** — Tüm sistem işlemlerinin detaylı kaydı (kim, ne, ne zaman, IP adresi)
- **AI Tahminleri** *(opsiyonel)* — Yarın tahmini satış/ciro, kritik stok tahmini

### 🔐 Güvenlik & Altyapı
- Rol tabanlı erişim kontrolü (Garson / Yönetici)
- Session tabanlı oturum yönetimi (60 dk zaman aşımı)
- SHA-256 ile şifre hashleme
- Kapsamlı işlem loglama (25 farklı işlem türü)
- Silme koruması (geçmişte kullanılmış kayıtlar pasife alınır, fiziksel silinmez)
- Decimal Model Binder (hem nokta hem virgül desteği — Türkçe uyumluluk)

## 📂 Proje Yapısı

```
RestoranYonetim/
├── Controllers/
│   ├── AdminController.cs      # Yönetici paneli (Dashboard, CRUD, Raporlar)
│   ├── AuthController.cs       # Giriş / Çıkış / Oturum yönetimi
│   ├── GarsonController.cs     # Garson paneli (Sipariş, Ödeme, Masa)
│   └── HomeController.cs       # Müşteri menüsü, geri bildirim, garson çağırma
├── Models/
│   ├── RestoranDbContext.cs     # EF Core DbContext (Fluent API)
│   ├── Personel.cs, Masa.cs, Urun.cs, Siparis.cs, Odeme.cs ...
│   └── AiModels.cs             # AI servis modelleri
├── Views/
│   ├── Admin/                  # Yönetici sayfaları (Index, Personel, Urunler, Rapor ...)
│   ├── Auth/                   # Giriş sayfası
│   ├── Garson/                 # Garson sayfaları (Index, Siparis, AktifSiparisler)
│   ├── Home/                   # Müşteri menü sayfası
│   └── Shared/                 # Layout dosyaları (_LayoutAdmin, _LayoutGarson, _LayoutMenu)
├── Helpers/
│   ├── LogHelper.cs            # İşlem log kayıt sistemi
│   ├── Sifreleme.cs            # SHA-256 şifreleme
│   ├── PdfRaporHelper.cs       # PDF rapor oluşturucu (iTextSharp)
│   └── DecimalModelBinder.cs   # Türkçe decimal uyumluluk
├── wwwroot/                    # Statik dosyalar (CSS, JS, görseller)
├── db.sql                      # Veritabanı oluşturma scripti (Tablo + Seed Data)
├── appsettings.json            # Bağlantı dizesi ve uygulama ayarları
└── Program.cs                  # Uygulama başlangıç yapılandırması
```

## 🗄️ Veritabanı Şeması

Proje **20 tablo** içermektedir:

| Tablo | Açıklama |
|-------|----------|
| `rol` | Personel rolleri (Garson, Yönetici) |
| `hesap_durum` | Hesap durumları (Aktif, Pasif, Askıda, İşten Ayrıldı) |
| `mesai_durum` | Mesai durumları (Mesai Dışı, Mesaide, Molada) |
| `personel` | Personel bilgileri ve kimlik doğrulama |
| `masa_durum` | Masa durumları (Boş, Dolu, Rezerve) |
| `masa` | Masa tanımları |
| `kategori` | Ürün kategorileri |
| `urun` | Ürün bilgileri (fiyat, stok, aktiflik) |
| `siparis` | Sipariş kayıtları |
| `siparis_durum` | Sipariş durumları (Hazırlanıyor, Servis Edildi, Tamamlandı, İptal Edildi) |
| `siparis_urun` | Sipariş-ürün ilişkisi (adet, birim fiyat) |
| `odeme_turu` | Ödeme türleri (Nakit, Kredi Kartı) |
| `odeme` | Ödeme kayıtları |
| `gider_kategori` | Gider kategorileri (Kira, Fatura, Maaş vb.) |
| `gider` | Gider kayıtları |
| `geribildirim_turu` | Geri bildirim türleri (Ürün, Genel, Hizmet) |
| `geribildirim` | Müşteri geri bildirimleri |
| `islem_turu` | İşlem log türleri (25 farklı tür) |
| `islem_log` | Sistem işlem logları |
| `gunluk_rapor` | Günlük rapor özeti |

## 🚀 Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express veya üstü)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (önerilen) veya VS Code

### Adım Adım Kurulum

**1. Projeyi klonlayın:**

```bash
git clone https://github.com/Rainyli00/RestoranYonetim.git
cd RestoranYonetim
```

**2. Veritabanını oluşturun:**

SQL Server Management Studio (SSMS) veya Azure Data Studio üzerinden `db.sql` dosyasını çalıştırın. Bu script:
- `restoran_db` veritabanını oluşturur
- Tüm tabloları ve indexleri oluşturur
- Varsayılan verileri (roller, durumlar, örnek menü, kullanıcılar) ekler

**3. Bağlantı dizesini güncelleyin:**

`RestoranYonetim/appsettings.json` dosyasında SQL Server bağlantı bilgilerinizi güncelleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SUNUCU_ADINIZ;Database=restoran_db;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**4. Projeyi çalıştırın:**

```bash
cd RestoranYonetim
dotnet run
```

### Varsayılan Giriş Bilgileri

| Rol | Kullanıcı Adı | Şifre |
|-----|---------------|-------|
| 🔑 Yönetici | `admin` | `admin123` |
| 👤 Garson | `garson` | `garson123` |

## 📦 Kullanılan NuGet Paketleri

| Paket | Sürüm | Açıklama |
|-------|-------|----------|
| `Microsoft.EntityFrameworkCore` | 8.0.11 | ORM framework |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.11 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.11 | EF Core CLI araçları |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.11 | Scaffold / Migration desteği |
| `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` | 8.0.11 | Geliştirme ortamında canlı Razor yenileme |
| `iTextSharp.LGPLv2.Core` | 3.7.12 | PDF rapor oluşturma |

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.
