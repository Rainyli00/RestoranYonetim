using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RestoranYonetim.Models;

public partial class RestoranDbContext : DbContext
{
    public RestoranDbContext()
    {
    }

    public RestoranDbContext(DbContextOptions<RestoranDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Geribildirim> Geribildirim { get; set; }

    public virtual DbSet<GeribildirimTuru> GeribildirimTuru { get; set; }

    public virtual DbSet<Gider> Gider { get; set; }

    public virtual DbSet<GiderKategori> GiderKategori { get; set; }

    public virtual DbSet<HesapDurum> HesapDurum { get; set; }

    public virtual DbSet<IslemLog> IslemLog { get; set; }

    public virtual DbSet<IslemTuru> IslemTuru { get; set; }

    public virtual DbSet<Kategori> Kategori { get; set; }

    public virtual DbSet<Masa> Masa { get; set; }

    public virtual DbSet<MasaDurum> MasaDurum { get; set; }

    public virtual DbSet<MesaiDurum> MesaiDurum { get; set; }

    public virtual DbSet<Odeme> Odeme { get; set; }

    public virtual DbSet<OdemeTuru> OdemeTuru { get; set; }

    public virtual DbSet<Personel> Personel { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<Siparis> Siparis { get; set; }

    public virtual DbSet<SiparisDurum> SiparisDurum { get; set; }

    public virtual DbSet<SiparisUrun> SiparisUrun { get; set; }

    public virtual DbSet<Urun> Urun { get; set; }

  
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Geribildirim>(entity =>
        {
            entity.HasKey(e => e.YorumId).HasName("PK__geribild__B0FA5C852E7DE0A4");

            entity.ToTable("geribildirim");

            entity.HasIndex(e => e.TurId, "IX_geribildirim_tur_id");

            entity.HasIndex(e => e.UrunId, "IX_geribildirim_urun_id");

            entity.Property(e => e.YorumId).HasColumnName("yorum_id");
            entity.Property(e => e.Puan).HasColumnName("puan");
            entity.Property(e => e.Tarih)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("tarih");
            entity.Property(e => e.TurId).HasColumnName("tur_id");
            entity.Property(e => e.UrunId).HasColumnName("urun_id");
            entity.Property(e => e.Yorum).HasColumnName("yorum");

            entity.HasOne(d => d.Tur).WithMany(p => p.Geribildirim)
                .HasForeignKey(d => d.TurId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_geribildirim_tur");

            entity.HasOne(d => d.Urun).WithMany(p => p.Geribildirim)
                .HasForeignKey(d => d.UrunId)
                .HasConstraintName("FK_geribildirim_urun");
        });

        modelBuilder.Entity<GeribildirimTuru>(entity =>
        {
            entity.HasKey(e => e.TurId).HasName("PK__geribild__DC915F621113BC71");

            entity.ToTable("geribildirim_turu");

            entity.HasIndex(e => e.TurAdi, "UQ_geribildirim_turu_adi").IsUnique();

            entity.Property(e => e.TurId).HasColumnName("tur_id");
            entity.Property(e => e.TurAdi)
                .HasMaxLength(50)
                .HasColumnName("tur_adi");
        });

        modelBuilder.Entity<Gider>(entity =>
        {
            entity.HasKey(e => e.GiderId).HasName("PK__gider__7DCC602D120A4BA7");

            entity.ToTable("gider");

            entity.HasIndex(e => e.GiderKategoriId, "IX_gider_gider_kategori_id");

            entity.HasIndex(e => e.GiderTarihi, "IX_gider_gider_tarihi");

            entity.HasIndex(e => e.PersonelId, "IX_gider_personel_id");

            entity.Property(e => e.GiderId).HasColumnName("gider_id");
            entity.Property(e => e.Aciklama)
                .HasMaxLength(255)
                .HasColumnName("aciklama");
            entity.Property(e => e.GiderKategoriId).HasColumnName("gider_kategori_id");
            entity.Property(e => e.GiderTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("gider_tarihi");
            entity.Property(e => e.PersonelId).HasColumnName("personel_id");
            entity.Property(e => e.Tutar)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("tutar");

            entity.HasOne(d => d.GiderKategori).WithMany(p => p.Gider)
                .HasForeignKey(d => d.GiderKategoriId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_gider_gider_kategori");

            entity.HasOne(d => d.Personel).WithMany(p => p.Gider)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_gider_personel");
        });

        modelBuilder.Entity<GiderKategori>(entity =>
        {
            entity.HasKey(e => e.GiderKategoriId).HasName("PK__gider_ka__9EA19299690B93DC");

            entity.ToTable("gider_kategori");

            entity.HasIndex(e => e.KategoriAdi, "UQ_gider_kategori_adi").IsUnique();

            entity.Property(e => e.GiderKategoriId).HasColumnName("gider_kategori_id");
            entity.Property(e => e.KategoriAdi)
                .HasMaxLength(50)
                .HasColumnName("kategori_adi");
        });

        modelBuilder.Entity<HesapDurum>(entity =>
        {
            entity.HasKey(e => e.HesapDurumId).HasName("PK__hesap_du__52B456632578B685");

            entity.ToTable("hesap_durum");

            entity.Property(e => e.HesapDurumId).HasColumnName("hesap_durum_id");
            entity.Property(e => e.DurumAdi)
                .HasMaxLength(50)
                .HasColumnName("durum_adi");
        });

        modelBuilder.Entity<IslemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__islem_lo__9E2397E0C101FEBB");

            entity.ToTable("islem_log");

            entity.HasIndex(e => e.IslemTuruId, "IX_islem_log_islem_turu_id");

            entity.HasIndex(e => e.PersonelId, "IX_islem_log_personel_id");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.IpAdresi)
                .HasMaxLength(45)
                .HasColumnName("ip_adresi");
            entity.Property(e => e.IslemAciklama)
                .HasMaxLength(500)
                .HasColumnName("islem_aciklama");
            entity.Property(e => e.IslemTuruId).HasColumnName("islem_turu_id");
            entity.Property(e => e.PersonelId).HasColumnName("personel_id");
            entity.Property(e => e.Zaman)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("zaman");

            entity.HasOne(d => d.IslemTuru).WithMany(p => p.IslemLog)
                .HasForeignKey(d => d.IslemTuruId)
                .HasConstraintName("FK_islem_log_islem_turu");

            entity.HasOne(d => d.Personel).WithMany(p => p.IslemLog)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_islem_log_personel");
        });

        modelBuilder.Entity<IslemTuru>(entity =>
        {
            entity.HasKey(e => e.IslemTuruId).HasName("PK__islem_tu__F7D6184C9C87818D");

            entity.ToTable("islem_turu");

            entity.HasIndex(e => e.TurAdi, "UQ__islem_tu__8E9B83CA64F99B02").IsUnique();

            entity.Property(e => e.IslemTuruId).HasColumnName("islem_turu_id");
            entity.Property(e => e.TurAdi)
                .HasMaxLength(50)
                .HasColumnName("tur_adi");
        });

        modelBuilder.Entity<Kategori>(entity =>
        {
            entity.HasKey(e => e.KategoriId).HasName("PK__kategori__AFB6FE70BE95E02F");

            entity.ToTable("kategori");

            entity.HasIndex(e => e.KategoriAdi, "UQ_kategori_adi").IsUnique();

            entity.Property(e => e.KategoriId).HasColumnName("kategori_id");
            entity.Property(e => e.KategoriAdi)
                .HasMaxLength(50)
                .HasColumnName("kategori_adi");
            entity.Property(e => e.ResimUrl)
                .HasMaxLength(255)
                .HasColumnName("resim_url");
            entity.Property(e => e.SiraNo).HasColumnName("sira_no");
        });

        modelBuilder.Entity<Masa>(entity =>
        {
            entity.HasKey(e => e.MasaId).HasName("PK__masa__A6F92F707A50B931");

            entity.ToTable("masa");

            entity.HasIndex(e => e.DurumId, "IX_masa_durum_id");

            entity.Property(e => e.MasaId).HasColumnName("masa_id");
            entity.Property(e => e.DurumId).HasColumnName("durum_id");
            entity.Property(e => e.MasaAdi)
                .HasMaxLength(20)
                .HasColumnName("masa_adi");

            entity.HasOne(d => d.Durum).WithMany(p => p.Masa)
                .HasForeignKey(d => d.DurumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_masa_masa_durum");
        });

        modelBuilder.Entity<MasaDurum>(entity =>
        {
            entity.HasKey(e => e.DurumId).HasName("PK__masa_dur__AC7848F0547AE60B");

            entity.ToTable("masa_durum");

            entity.Property(e => e.DurumId).HasColumnName("durum_id");
            entity.Property(e => e.DurumAdi)
                .HasMaxLength(50)
                .HasColumnName("durum_adi");
        });

        modelBuilder.Entity<MesaiDurum>(entity =>
        {
            entity.HasKey(e => e.MesaiDurumId).HasName("PK__mesai_du__8BDE8462698D39AD");

            entity.ToTable("mesai_durum");

            entity.Property(e => e.MesaiDurumId).HasColumnName("mesai_durum_id");
            entity.Property(e => e.DurumAdi)
                .HasMaxLength(50)
                .HasColumnName("durum_adi");
        });

        modelBuilder.Entity<Odeme>(entity =>
        {
            entity.HasKey(e => e.OdemeId).HasName("PK__odeme__3074695962A987FA");

            entity.ToTable("odeme");

            entity.HasIndex(e => e.PersonelId, "IX_odeme_personel_id");

            entity.HasIndex(e => e.SiparisId, "IX_odeme_siparis_id");

            entity.HasIndex(e => e.Tarih, "IX_odeme_tarih");

            entity.Property(e => e.OdemeId).HasColumnName("odeme_id");
            entity.Property(e => e.OdemeTuruId).HasColumnName("odeme_turu_id");
            entity.Property(e => e.PersonelId).HasColumnName("personel_id");
            entity.Property(e => e.SiparisId).HasColumnName("siparis_id");
            entity.Property(e => e.Tarih)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("tarih");
            entity.Property(e => e.Tutar)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("tutar");

            entity.HasOne(d => d.OdemeTuru).WithMany(p => p.Odeme)
                .HasForeignKey(d => d.OdemeTuruId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_odeme_odeme_turu");

            entity.HasOne(d => d.Personel).WithMany(p => p.Odeme)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_odeme_personel");

            entity.HasOne(d => d.Siparis).WithMany(p => p.Odeme)
                .HasForeignKey(d => d.SiparisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_odeme_siparis");
        });

        modelBuilder.Entity<OdemeTuru>(entity =>
        {
            entity.HasKey(e => e.OdemeTuruId).HasName("PK__odeme_tu__0110DC82E05A5668");

            entity.ToTable("odeme_turu");

            entity.HasIndex(e => e.OdemeTuruAdi, "UQ_odeme_turu_adi").IsUnique();

            entity.Property(e => e.OdemeTuruId).HasColumnName("odeme_turu_id");
            entity.Property(e => e.OdemeTuruAdi)
                .HasMaxLength(30)
                .HasColumnName("odeme_turu_adi");
        });

        modelBuilder.Entity<Personel>(entity =>
        {
            entity.HasKey(e => e.PersonelId).HasName("PK__personel__48A5539F95A5CFF1");

            entity.ToTable("personel");

            entity.HasIndex(e => e.Email, "IX_personel_email");

            entity.HasIndex(e => e.HesapDurumId, "IX_personel_hesap_durum_id");

            entity.HasIndex(e => e.MesaiDurumId, "IX_personel_mesai_durum_id");

            entity.HasIndex(e => e.RolId, "IX_personel_rol_id");

            entity.HasIndex(e => e.Telefon, "IX_personel_telefon");

            entity.HasIndex(e => e.KullaniciAdi, "UQ_personel_kullanici_adi").IsUnique();

            entity.Property(e => e.PersonelId).HasColumnName("personel_id");
            entity.Property(e => e.AdSoyad)
                .HasMaxLength(100)
                .HasColumnName("ad_soyad");
            entity.Property(e => e.Adres)
                .HasMaxLength(500)
                .HasColumnName("adres");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.HesapDurumId).HasColumnName("hesap_durum_id");
            entity.Property(e => e.KullaniciAdi)
                .HasMaxLength(60)
                .HasColumnName("kullanici_adi");
            entity.Property(e => e.MesaiDurumId)
                .HasDefaultValue(1)
                .HasColumnName("mesai_durum_id");
            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.SifreHash)
                .HasMaxLength(255)
                .HasColumnName("sifre_hash");
            entity.Property(e => e.SonGirisTarihi)
                .HasColumnType("datetime")
                .HasColumnName("son_giris_tarihi");
            entity.Property(e => e.Telefon)
                .HasMaxLength(20)
                .HasColumnName("telefon");

            entity.HasOne(d => d.HesapDurum).WithMany(p => p.Personel)
                .HasForeignKey(d => d.HesapDurumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_personel_hesap_durum");

            entity.HasOne(d => d.MesaiDurum).WithMany(p => p.Personel)
                .HasForeignKey(d => d.MesaiDurumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_personel_mesai_durum");

            entity.HasOne(d => d.Rol).WithMany(p => p.Personel)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_personel_rol");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__rol__CF32E443DABC2E7E");

            entity.ToTable("rol");

            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.RolAdi)
                .HasMaxLength(50)
                .HasColumnName("rol_adi");
        });

        modelBuilder.Entity<Siparis>(entity =>
        {
            entity.HasKey(e => e.SiparisId).HasName("PK__siparis__BB5E5880DDE6E9F4");

            entity.ToTable("siparis");

            entity.HasIndex(e => e.GarsonId, "IX_siparis_garson_id");

            entity.HasIndex(e => e.MasaId, "IX_siparis_masa_id");

            entity.HasIndex(e => e.OlusturmaTarihi, "IX_siparis_olusturma_tarihi");

            entity.HasIndex(e => e.SiparisDurumId, "IX_siparis_siparis_durum_id");

            entity.Property(e => e.SiparisId).HasColumnName("siparis_id");
            entity.Property(e => e.GarsonId).HasColumnName("garson_id");
            entity.Property(e => e.KapatmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("kapatma_tarihi");
            entity.Property(e => e.MasaId).HasColumnName("masa_id");
            entity.Property(e => e.Notlar).HasColumnName("notlar");
            entity.Property(e => e.OlusturmaTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.SiparisDurumId).HasColumnName("siparis_durum_id");

            entity.HasOne(d => d.Garson).WithMany(p => p.Siparis)
                .HasForeignKey(d => d.GarsonId)
                .HasConstraintName("FK_siparis_garson");

            entity.HasOne(d => d.Masa).WithMany(p => p.Siparis)
                .HasForeignKey(d => d.MasaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_siparis_masa");

            entity.HasOne(d => d.SiparisDurum).WithMany(p => p.Siparis)
                .HasForeignKey(d => d.SiparisDurumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_siparis_siparis_durum");
        });

        modelBuilder.Entity<SiparisDurum>(entity =>
        {
            entity.HasKey(e => e.SiparisDurumId).HasName("PK__siparis___9E928F91E957DD51");

            entity.ToTable("siparis_durum");

            entity.Property(e => e.SiparisDurumId).HasColumnName("siparis_durum_id");
            entity.Property(e => e.DurumAdi)
                .HasMaxLength(50)
                .HasColumnName("durum_adi");
        });

        modelBuilder.Entity<SiparisUrun>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__siparis___3213E83F68C679CD");

            entity.ToTable("siparis_urun");

            entity.HasIndex(e => e.SiparisId, "IX_siparis_urun_siparis_id");

            entity.HasIndex(e => e.UrunId, "IX_siparis_urun_urun_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Adet).HasColumnName("adet");
            entity.Property(e => e.BirimFiyat)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("birim_fiyat");
            entity.Property(e => e.SiparisId).HasColumnName("siparis_id");
            entity.Property(e => e.UrunId).HasColumnName("urun_id");

            entity.HasOne(d => d.Siparis).WithMany(p => p.SiparisUrun)
                .HasForeignKey(d => d.SiparisId)
                .HasConstraintName("FK_siparis_urun_siparis");

            entity.HasOne(d => d.Urun).WithMany(p => p.SiparisUrun)
                .HasForeignKey(d => d.UrunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_siparis_urun_urun");
        });

        modelBuilder.Entity<Urun>(entity =>
        {
            entity.HasKey(e => e.UrunId).HasName("PK__urun__933C200A22FBA3B7");

            entity.ToTable("urun");

            entity.HasIndex(e => e.AktifMi, "IX_urun_aktif_mi");

            entity.HasIndex(e => e.KategoriId, "IX_urun_kategori_id");

            entity.HasIndex(e => new { e.KategoriId, e.UrunAdi }, "UQ_urun_kategori_adi").IsUnique();

            entity.Property(e => e.UrunId).HasColumnName("urun_id");
            entity.Property(e => e.AktifMi)
                .HasDefaultValue((byte)1)
                .HasColumnName("aktif_mi");
            entity.Property(e => e.Fiyat)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("fiyat");
            entity.Property(e => e.KategoriId).HasColumnName("kategori_id");
            entity.Property(e => e.ResimUrl)
                .HasMaxLength(255)
                .HasColumnName("resim_url");
            entity.Property(e => e.Stok).HasColumnName("stok");
            entity.Property(e => e.UrunAdi)
                .HasMaxLength(100)
                .HasColumnName("urun_adi");

            entity.HasOne(d => d.Kategori).WithMany(p => p.Urun)
                .HasForeignKey(d => d.KategoriId)
                .HasConstraintName("FK_urun_kategori");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
