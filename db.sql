------------------------------------------------------------
-- RESTORAN_DB - DERS PROJESÝ SÜRÜMÜ (MSSQL) - GÜNCELLENMÝÞ
------------------------------------------------------------
IF DB_ID('restoran_db') IS NOT NULL
BEGIN
    ALTER DATABASE restoran_db SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE restoran_db;
END
GO

CREATE DATABASE restoran_db;
GO

USE restoran_db;
GO

------------------------------------------------------------
-- TABLO OLUÞTURMA
------------------------------------------------------------

-- 1) rol
CREATE TABLE dbo.rol (
    rol_id   INT IDENTITY(1,1) PRIMARY KEY,
    rol_adi  NVARCHAR(50) NOT NULL
);
GO

-- 2) hesap_durum
CREATE TABLE dbo.hesap_durum (
    hesap_durum_id INT IDENTITY(1,1) PRIMARY KEY,
    durum_adi      NVARCHAR(50) NOT NULL
);
GO

-- 3) masa_durum
CREATE TABLE dbo.masa_durum (
    durum_id  INT IDENTITY(1,1) PRIMARY KEY,
    durum_adi NVARCHAR(50) NOT NULL
);
GO

-- [YENÝ EKLENDÝ] 4) mesai_durum
CREATE TABLE dbo.mesai_durum (
    mesai_durum_id INT IDENTITY(1,1) PRIMARY KEY,
    durum_adi      NVARCHAR(50) NOT NULL
);
GO

-- 5) kategori
CREATE TABLE dbo.kategori (
    kategori_id  INT IDENTITY(1,1) PRIMARY KEY,
    kategori_adi NVARCHAR(50) NOT NULL,
    sira_no      INT NOT NULL CONSTRAINT DF_kategori_sira_no DEFAULT 0,
    resim_url    NVARCHAR(255) NULL,
    CONSTRAINT UQ_kategori_adi UNIQUE (kategori_adi)
);
GO

-- 6) siparis_durum
CREATE TABLE dbo.siparis_durum (
    siparis_durum_id INT IDENTITY(1,1) PRIMARY KEY,
    durum_adi        NVARCHAR(50) NOT NULL
);
GO

-- 7) odeme_turu
CREATE TABLE dbo.odeme_turu (
    odeme_turu_id  INT IDENTITY(1,1) PRIMARY KEY,
    odeme_turu_adi NVARCHAR(30) NOT NULL,
    CONSTRAINT UQ_odeme_turu_adi UNIQUE (odeme_turu_adi)
);
GO

-- 8) geribildirim_turu
CREATE TABLE dbo.geribildirim_turu (
    tur_id  INT IDENTITY(1,1) PRIMARY KEY,
    tur_adi NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_geribildirim_turu_adi UNIQUE (tur_adi)
);
GO

-- 9) gider_kategori
CREATE TABLE dbo.gider_kategori (
    gider_kategori_id INT IDENTITY(1,1) PRIMARY KEY,
    kategori_adi      NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_gider_kategori_adi UNIQUE (kategori_adi)
);
GO

-- 10) personel (GÜNCELLENDÝ)
CREATE TABLE dbo.personel (
    personel_id      INT IDENTITY(1,1) PRIMARY KEY,
    ad_soyad         NVARCHAR(100) NOT NULL,
    telefon          NVARCHAR(20) NULL,
    email            NVARCHAR(255) NULL,
    adres            NVARCHAR(500) NULL,
    kullanici_adi    NVARCHAR(60)  NOT NULL,
    sifre_hash       NVARCHAR(255) NOT NULL,
    rol_id           INT           NOT NULL,
    hesap_durum_id   INT           NOT NULL,
    mesai_durum_id   INT           NOT NULL DEFAULT 1, -- Yeni Kolon (Default 1: Mesai Dýþý)
    son_giris_tarihi DATETIME      NULL,
    CONSTRAINT UQ_personel_kullanici_adi UNIQUE (kullanici_adi),
    CONSTRAINT FK_personel_rol FOREIGN KEY (rol_id) REFERENCES dbo.rol(rol_id),
    CONSTRAINT FK_personel_hesap_durum FOREIGN KEY (hesap_durum_id) REFERENCES dbo.hesap_durum(hesap_durum_id),
    CONSTRAINT FK_personel_mesai_durum FOREIGN KEY (mesai_durum_id) REFERENCES dbo.mesai_durum(mesai_durum_id) -- Yeni Baðlantý
);
GO

-- 11) masa
CREATE TABLE dbo.masa (
    masa_id  INT IDENTITY(1,1) PRIMARY KEY,
    masa_adi NVARCHAR(20) NOT NULL,
    durum_id INT NOT NULL,
    CONSTRAINT FK_masa_masa_durum FOREIGN KEY (durum_id) REFERENCES dbo.masa_durum(durum_id)
);
GO

-- 12) urun
CREATE TABLE dbo.urun (
    urun_id     INT IDENTITY(1,1) PRIMARY KEY,
    kategori_id INT            NULL,
    urun_adi    NVARCHAR(100)  NOT NULL,
    fiyat       DECIMAL(10,2)  NOT NULL CONSTRAINT CK_urun_fiyat CHECK (fiyat >= 0),
    stok        INT            NOT NULL CONSTRAINT CK_urun_stok  CHECK (stok >= 0),
    aktif_mi    TINYINT        NOT NULL CONSTRAINT DF_urun_aktif_mi DEFAULT 1,
    resim_url   NVARCHAR(255)  NULL,
    CONSTRAINT UQ_urun_kategori_adi UNIQUE (kategori_id, urun_adi),
    CONSTRAINT FK_urun_kategori FOREIGN KEY (kategori_id) REFERENCES dbo.kategori(kategori_id)
);
GO

-- 13) siparis
CREATE TABLE dbo.siparis (
    siparis_id       INT IDENTITY(1,1) PRIMARY KEY,
    masa_id          INT           NULL,
    garson_id        INT           NULL,
    siparis_durum_id INT           NOT NULL,
    notlar           NVARCHAR(MAX) NULL,
    olusturma_tarihi DATETIME      NOT NULL CONSTRAINT DF_siparis_olusturma_tarihi DEFAULT GETDATE(),
    kapatma_tarihi   DATETIME      NULL,
    CONSTRAINT FK_siparis_masa FOREIGN KEY (masa_id) REFERENCES dbo.masa(masa_id) ON DELETE SET NULL,
    CONSTRAINT FK_siparis_garson FOREIGN KEY (garson_id) REFERENCES dbo.personel(personel_id),
    CONSTRAINT FK_siparis_siparis_durum FOREIGN KEY (siparis_durum_id) REFERENCES dbo.siparis_durum(siparis_durum_id)
);
GO

-- 14) siparis_urun
CREATE TABLE dbo.siparis_urun (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    siparis_id  INT            NOT NULL,
    urun_id     INT            NOT NULL,
    adet        INT            NOT NULL CONSTRAINT CK_siparis_urun_adet CHECK (adet > 0),
    birim_fiyat DECIMAL(10,2) NOT NULL CONSTRAINT CK_siparis_urun_birim_fiyat CHECK (birim_fiyat >= 0),
    CONSTRAINT FK_siparis_urun_siparis FOREIGN KEY (siparis_id) REFERENCES dbo.siparis(siparis_id) ON DELETE CASCADE,
    CONSTRAINT FK_siparis_urun_urun FOREIGN KEY (urun_id) REFERENCES dbo.urun(urun_id)
);
GO

-- 15) odeme
CREATE TABLE dbo.odeme (
    odeme_id      INT IDENTITY(1,1) PRIMARY KEY,
    siparis_id    INT            NOT NULL,
    odeme_turu_id INT            NOT NULL,
    personel_id   INT            NULL,
    tutar         DECIMAL(12,2) NOT NULL CONSTRAINT CK_odeme_tutar CHECK (tutar >= 0),
    tarih         DATETIME      NOT NULL CONSTRAINT DF_odeme_tarih DEFAULT GETDATE(),
    CONSTRAINT FK_odeme_siparis FOREIGN KEY (siparis_id) REFERENCES dbo.siparis(siparis_id),
    CONSTRAINT FK_odeme_odeme_turu FOREIGN KEY (odeme_turu_id) REFERENCES dbo.odeme_turu(odeme_turu_id),
    CONSTRAINT FK_odeme_personel FOREIGN KEY (personel_id) REFERENCES dbo.personel(personel_id)
);
GO

-- 16) geribildirim
CREATE TABLE dbo.geribildirim (
    yorum_id INT IDENTITY(1,1) PRIMARY KEY,
    tur_id   INT           NOT NULL,
    urun_id  INT           NULL,
    puan     TINYINT       NOT NULL CONSTRAINT CK_geribildirim_puan CHECK (puan BETWEEN 1 AND 5),
    yorum    NVARCHAR(MAX) NULL,
    tarih    DATETIME      NOT NULL CONSTRAINT DF_geribildirim_tarih DEFAULT GETDATE(),
    CONSTRAINT FK_geribildirim_tur FOREIGN KEY (tur_id) REFERENCES dbo.geribildirim_turu(tur_id),
    CONSTRAINT FK_geribildirim_urun FOREIGN KEY (urun_id) REFERENCES dbo.urun(urun_id)
);
GO

-- 17) gunluk_rapor
CREATE TABLE dbo.gunluk_rapor (
    tarih               DATE          PRIMARY KEY,
    toplam_siparis      INT           NOT NULL CONSTRAINT DF_gunluk_rapor_toplam_siparis DEFAULT 0,
    toplam_gelir        DECIMAL(14,2) NOT NULL CONSTRAINT DF_gunluk_rapor_toplam_gelir DEFAULT 0,
    toplam_gider        DECIMAL(14,2) NOT NULL CONSTRAINT DF_gunluk_rapor_toplam_gider DEFAULT 0,
    net_kar             DECIMAL(14,2) NOT NULL CONSTRAINT DF_gunluk_rapor_net_kar DEFAULT 0,
    en_populer_urun_adi NVARCHAR(100)  NULL,
    en_populer_adet     INT           NOT NULL CONSTRAINT DF_gunluk_rapor_en_populer_adet DEFAULT 0
);
GO

-- 18) islem_turu
CREATE TABLE dbo.islem_turu (
    islem_turu_id INT IDENTITY(1,1) PRIMARY KEY,
    tur_adi       NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- 19) islem_log
CREATE TABLE dbo.islem_log (
    log_id        INT IDENTITY(1,1) PRIMARY KEY,
    personel_id   INT           NULL,
    islem_turu_id INT           NULL,
    ip_adresi     NVARCHAR(45) NULL,
    zaman         DATETIME      NOT NULL CONSTRAINT DF_islem_log_zaman DEFAULT GETDATE(),
    CONSTRAINT FK_islem_log_personel FOREIGN KEY (personel_id) REFERENCES dbo.personel(personel_id),
    CONSTRAINT FK_islem_log_islem_turu FOREIGN KEY (islem_turu_id) REFERENCES dbo.islem_turu(islem_turu_id)
);
GO

-- 20) gider
CREATE TABLE dbo.gider (
    gider_id          INT IDENTITY(1,1) PRIMARY KEY,
    gider_kategori_id INT            NOT NULL,
    personel_id       INT            NULL,
    aciklama          NVARCHAR(255)  NULL,
    tutar             DECIMAL(12,2) NOT NULL CONSTRAINT CK_gider_tutar CHECK (tutar >= 0),
    gider_tarihi      DATETIME      NOT NULL CONSTRAINT DF_gider_tarihi DEFAULT GETDATE(),
    CONSTRAINT FK_gider_gider_kategori FOREIGN KEY (gider_kategori_id) REFERENCES dbo.gider_kategori(gider_kategori_id),
    CONSTRAINT FK_gider_personel FOREIGN KEY (personel_id) REFERENCES dbo.personel(personel_id)
);
GO

------------------------------------------------------------
-- INDEXLER (PERFORMANS)
------------------------------------------------------------

-- PERSONEL
CREATE NONCLUSTERED INDEX IX_personel_rol_id ON dbo.personel(rol_id);
CREATE NONCLUSTERED INDEX IX_personel_email ON dbo.personel(email);
CREATE NONCLUSTERED INDEX IX_personel_telefon ON dbo.personel(telefon);
CREATE NONCLUSTERED INDEX IX_personel_hesap_durum_id ON dbo.personel(hesap_durum_id);
CREATE NONCLUSTERED INDEX IX_personel_mesai_durum_id ON dbo.personel(mesai_durum_id); -- GÜNCELLENDÝ

-- MASA
CREATE NONCLUSTERED INDEX IX_masa_durum_id ON dbo.masa(durum_id);

-- URUN
CREATE NONCLUSTERED INDEX IX_urun_kategori_id ON dbo.urun(kategori_id);
CREATE NONCLUSTERED INDEX IX_urun_aktif_mi ON dbo.urun(aktif_mi);

-- SIPARIS
CREATE NONCLUSTERED INDEX IX_siparis_masa_id ON dbo.siparis(masa_id);
CREATE NONCLUSTERED INDEX IX_siparis_garson_id ON dbo.siparis(garson_id);
CREATE NONCLUSTERED INDEX IX_siparis_siparis_durum_id ON dbo.siparis(siparis_durum_id);
CREATE NONCLUSTERED INDEX IX_siparis_olusturma_tarihi ON dbo.siparis(olusturma_tarihi);

-- SIPARIS_URUN
CREATE NONCLUSTERED INDEX IX_siparis_urun_siparis_id ON dbo.siparis_urun(siparis_id);
CREATE NONCLUSTERED INDEX IX_siparis_urun_urun_id ON dbo.siparis_urun(urun_id);

-- ODEME
CREATE NONCLUSTERED INDEX IX_odeme_siparis_id ON dbo.odeme(siparis_id);
CREATE NONCLUSTERED INDEX IX_odeme_personel_id ON dbo.odeme(personel_id);
CREATE NONCLUSTERED INDEX IX_odeme_tarih ON dbo.odeme(tarih);

-- GERIBILDIRIM
CREATE NONCLUSTERED INDEX IX_geribildirim_tur_id ON dbo.geribildirim(tur_id);
CREATE NONCLUSTERED INDEX IX_geribildirim_urun_id ON dbo.geribildirim(urun_id);

-- GIDER
CREATE NONCLUSTERED INDEX IX_gider_gider_kategori_id ON dbo.gider(gider_kategori_id);
CREATE NONCLUSTERED INDEX IX_gider_personel_id ON dbo.gider(personel_id);
CREATE NONCLUSTERED INDEX IX_gider_gider_tarihi ON dbo.gider(gider_tarihi);

-- ISLEM_LOG
CREATE NONCLUSTERED INDEX IX_islem_log_personel_id ON dbo.islem_log(personel_id);
CREATE NONCLUSTERED INDEX IX_islem_log_islem_turu_id ON dbo.islem_log(islem_turu_id);

GO

------------------------------------------------------------
-- VARSAYILAN VERÝLER (SEED DATA)
------------------------------------------------------------
-- Mesai Durumlarý (Burasý önemli, personel ekleyebilmen için dolu olmalý)
INSERT INTO dbo.mesai_durum (durum_adi) VALUES 
('Mesai Dýþý'),
('Mesaide'),
('Molada');
GO

PRINT 'restoran_db baþarýyla oluþturuldu ve mesai_durum tablosu eklendi.';
GO