# -*- coding: utf-8 -*-
"""
Restoran Yönetim Sistemi - AI Tahmin Servisi
============================================
Bu servis, restoran verilerini analiz ederek tahminler üretir.

Çalıştırmak için: uvicorn ai_service:app --reload --port 8000
"""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import pyodbc
import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.ensemble import RandomForestRegressor
import numpy as np
from datetime import datetime, timedelta
import warnings

# Pandas ve sklearn uyarılarını sustur
warnings.filterwarnings('ignore', category=UserWarning)

app = FastAPI(
    title="Restoran AI Tahmin Servisi",
    description="Satış tahmini, trend analizi ve stok yönetimi için AI servisi",
    version="2.0.0"
)

# CORS ayarları (C# tarafından erişim için)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- VERITABANI AYARLARI ---
DB_CONFIG = (
    "Driver={SQL Server};"
    "Server=.;"
    "Database=restoran_db;"
    "Trusted_Connection=yes;"
)

def get_db_connection():
    """Veritabanı bağlantısı oluştur"""
    return pyodbc.connect(DB_CONFIG)


# ============================================================
# 1. YARINKI SATIŞ TAHMİNİ (Geliştirilmiş)
# ============================================================
@app.get("/tahmin/yarin")
def tahmin_yarin():
    """
    Yarınki satış adedini ve cirosunu tahmin eder.
    Haftanın günü bazlı ortalama + trend analizi kullanır.
    """
    try:
        conn = get_db_connection()
        
        # Günlük satış verileri
        query = """
        SELECT
            CAST(s.olusturma_tarihi AS DATE) as Gun,
            DATEPART(WEEKDAY, s.olusturma_tarihi) as HaftaninGunu,
            COUNT(DISTINCT s.siparis_id) as SiparisSayisi,
            SUM(su.adet) as SatisAdedi,
            SUM(su.adet * su.birim_fiyat) as Ciro
        FROM siparis s
        INNER JOIN siparis_urun su ON s.siparis_id = su.siparis_id
        WHERE s.siparis_durum_id != 4
        GROUP BY CAST(s.olusturma_tarihi AS DATE), DATEPART(WEEKDAY, s.olusturma_tarihi)
        ORDER BY Gun ASC
        """
        
        df = pd.read_sql(query, conn)
        conn.close()
        
        if df.empty or len(df) < 3:
            return {
                "basari": False,
                "mesaj": "Yeterli veri yok (en az 3 gün gerekli)",
                "tahmini_satis": 0,
                "tahmini_ciro": 0
            }
        
        # Yarın için tahmin
        yarin = datetime.now() + timedelta(days=1)
        yarin_haftanin_gunu = yarin.isoweekday()  # 1=Pazartesi, 7=Pazar
        
        # Aynı haftanın günü için geçmiş verileri bul
        ayni_gun_verileri = df[df['HaftaninGunu'] == yarin_haftanin_gunu]
        
        # Son 7 günün ortalaması
        son_7_gun = df.tail(7)
        ortalama_satis_7gun = int(son_7_gun['SatisAdedi'].mean())
        ortalama_ciro_7gun = float(son_7_gun['Ciro'].mean())
        
        if len(ayni_gun_verileri) >= 2:
            # Aynı gün için yeterli veri varsa, o günün ortalamasını kullan
            tahmini_satis = int(ayni_gun_verileri['SatisAdedi'].mean())
            tahmini_ciro = float(ayni_gun_verileri['Ciro'].mean())
            tahmin_yontemi = "Haftanın günü ortalaması"
        else:
            # Yeterli veri yoksa, son 7 günün ortalamasını kullan
            tahmini_satis = ortalama_satis_7gun
            tahmini_ciro = ortalama_ciro_7gun
            tahmin_yontemi = "Son 7 gün ortalaması"
        
        # Trend faktörü (son 3 gün / önceki 3 gün)
        if len(df) >= 6:
            son_3 = df.tail(3)['Ciro'].mean()
            onceki_3 = df.tail(6).head(3)['Ciro'].mean()
            if onceki_3 > 0:
                trend_faktoru = son_3 / onceki_3
                # Trend faktörünü sınırla (0.5 - 1.5 arası)
                trend_faktoru = max(0.5, min(1.5, trend_faktoru))
                tahmini_ciro = tahmini_ciro * trend_faktoru
                tahmini_satis = int(tahmini_satis * trend_faktoru)
        
        # Minimum değerleri uygula (negatif olmasın)
        tahmini_satis = max(1, tahmini_satis)
        tahmini_ciro = max(100, tahmini_ciro)  # En az 100 TL
        
        # Değişim yüzdesi
        satis_degisim = ((tahmini_satis - ortalama_satis_7gun) / ortalama_satis_7gun * 100) if ortalama_satis_7gun > 0 else 0
        ciro_degisim = ((tahmini_ciro - ortalama_ciro_7gun) / ortalama_ciro_7gun * 100) if ortalama_ciro_7gun > 0 else 0
        
        return {
            "basari": True,
            "tarih": yarin.strftime("%Y-%m-%d"),
            "gun_adi": ["Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar"][yarin_haftanin_gunu - 1],
            "tahmini_satis": tahmini_satis,
            "tahmini_ciro": round(tahmini_ciro, 2),
            "ortalama_satis_7gun": ortalama_satis_7gun,
            "ortalama_ciro_7gun": round(ortalama_ciro_7gun, 2),
            "satis_degisim_yuzde": round(satis_degisim, 1),
            "ciro_degisim_yuzde": round(ciro_degisim, 1)
        }
        
    except Exception as e:
        return {"basari": False, "hata": str(e)}


# ============================================================
# 2. STOK TÜKENME TAHMİNİ
# ============================================================
@app.get("/tahmin/stok")
def stok_tukenme_tahmini():
    """
    Her ürün için stok tükenme tarihini tahmin eder.
    Son 30 günlük satış hızına göre hesaplar.
    """
    try:
        conn = get_db_connection()
        
        # Son 30 günlük ürün bazlı satışlar
        query = """
        SELECT 
            u.urun_id as UrunId,
            u.urun_adi as UrunAdi,
            u.stok as MevcutStok,
            k.kategori_adi as KategoriAdi,
            ISNULL(SUM(su.adet), 0) as Son30GunSatis
        FROM urun u
        LEFT JOIN kategori k ON u.kategori_id = k.kategori_id
        LEFT JOIN siparis_urun su ON u.urun_id = su.urun_id
        LEFT JOIN siparis s ON su.siparis_id = s.siparis_id 
            AND s.siparis_durum_id != 4
            AND s.olusturma_tarihi >= DATEADD(DAY, -30, GETDATE())
        WHERE u.aktif_mi = 1
        GROUP BY u.urun_id, u.urun_adi, u.stok, k.kategori_adi
        ORDER BY u.stok ASC
        """
        
        df = pd.read_sql(query, conn)
        conn.close()
        
        if df.empty:
            return {"basari": False, "mesaj": "Ürün bulunamadı"}
        
        urunler = []
        kritik_urunler = []
        
        for _, row in df.iterrows():
            gunluk_satis = row['Son30GunSatis'] / 30  # Günlük ortalama satış
            mevcut_stok = row['MevcutStok']
            
            if gunluk_satis > 0:
                kalan_gun = int(mevcut_stok / gunluk_satis)
                tukenme_tarihi = (datetime.now() + timedelta(days=kalan_gun)).strftime("%Y-%m-%d")
            else:
                kalan_gun = 999  # Satış yok, tükenmeyecek
                tukenme_tarihi = "Belirsiz (satış yok)"
            
            # Aciliyet durumu
            if mevcut_stok == 0:
                aciliyet = "Tükendi"
                aciliyet_renk = "red"
            elif kalan_gun <= 3:
                aciliyet = "Kritik"
                aciliyet_renk = "red"
            elif kalan_gun <= 7:
                aciliyet = "Uyarı"
                aciliyet_renk = "orange"
            elif kalan_gun <= 14:
                aciliyet = "Dikkat"
                aciliyet_renk = "yellow"
            else:
                aciliyet = "Normal"
                aciliyet_renk = "green"
            
            urun_data = {
                "urun_id": int(row['UrunId']),
                "urun_adi": row['UrunAdi'],
                "kategori": row['KategoriAdi'] or "Kategorisiz",
                "mevcut_stok": int(mevcut_stok),
                "gunluk_satis_ortalama": round(gunluk_satis, 2),
                "tahmini_kalan_gun": kalan_gun if kalan_gun < 999 else None,
                "tahmini_tukenme_tarihi": tukenme_tarihi,
                "aciliyet": aciliyet,
                "aciliyet_renk": aciliyet_renk
            }
            
            urunler.append(urun_data)
            
            # Kritik ürünleri ayır (7 gün içinde tükenecekler)
            if kalan_gun <= 7:
                kritik_urunler.append(urun_data)
        
        # Stoğu en kritik olandan başla
        urunler.sort(key=lambda x: x['tahmini_kalan_gun'] if x['tahmini_kalan_gun'] else 999)
        kritik_urunler.sort(key=lambda x: x['tahmini_kalan_gun'] if x['tahmini_kalan_gun'] else 999)
        
        return {
            "basari": True,
            "analiz_tarihi": datetime.now().strftime("%Y-%m-%d %H:%M"),
            "toplam_urun": len(urunler),
            "kritik_urun_sayisi": len(kritik_urunler),
            "tukenen_urun_sayisi": len([u for u in urunler if u['mevcut_stok'] == 0]),
            "kritik_urunler": kritik_urunler[:10],  # İlk 10 kritik ürün
            "tum_urunler": urunler
        }
        
    except Exception as e:
        return {"basari": False, "hata": str(e)}




# ============================================================
# SAĞLIK KONTROLÜ
# ============================================================

@app.get("/health")
def health_check():
    """Servis durumu kontrolü"""
    try:
        conn = get_db_connection()
        conn.close()
        return {"status": "healthy", "database": "connected"}
    except Exception as e:
        return {"status": "unhealthy", "database": "disconnected", "error": str(e)}


# Çalıştırmak için: uvicorn ai_service:app --reload --port 8000