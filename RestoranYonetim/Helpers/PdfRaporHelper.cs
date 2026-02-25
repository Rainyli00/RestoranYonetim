using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using System.IO;

namespace RestoranYonetim.Helpers
{
    public class PdfRaporHelper
    {
        // Türkçe karakter desteði için font
        private static readonly string FONT_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        public static byte[] SatisRaporuOlustur(
            DateTime baslangic,
            DateTime bitis,
            decimal toplamCiro,
            decimal toplamGider,
            decimal netKar,
            int siparisSayisi,
            decimal ortalamaSiparisDegeri,
            List<(string UrunAdi, int Adet)> enCokSatanlar,
            List<(string Kategori, decimal Tutar)> giderDagilimi,
            List<(string PersonelAdi, decimal ToplamCiro, int SiparisSayisi)> personelPerformansi,
            List<(string Kategori, decimal ToplamSatis)> kategoriSatislari,
            int iptalEdilenSiparisSayisi,
            double iptalOrani,
            (DateTime Tarih, decimal Tutar)? enYuksekGun,
            List<(string OdemeTuruAdi, int Adet, decimal Tutar)>? odemeYontemleri = null,
            string raporTuru = "Genel")
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // PDF Döküman Oluþtur
                Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Font Ayarlarý
                BaseFont bfTurkce = BaseFont.CreateFont(FONT_PATH, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font baslikFont = new Font(bfTurkce, 18, Font.BOLD, new BaseColor(64, 64, 64));
                Font altBaslikFont = new Font(bfTurkce, 14, Font.BOLD, BaseColor.Black);
                Font normalFont = new Font(bfTurkce, 10, Font.NORMAL, BaseColor.Black);
                Font kucukFont = new Font(bfTurkce, 8, Font.NORMAL, BaseColor.Gray);
                Font kalinFont = new Font(bfTurkce, 10, Font.BOLD, BaseColor.Black);

                // BAÞLIK BÖLÜMÜ
                Paragraph baslik = new Paragraph($"{raporTuru} Satýþ Raporu", baslikFont);
                baslik.Alignment = Element.ALIGN_CENTER;
                baslik.SpacingAfter = 10f;
                document.Add(baslik);

                // Tarih Aralýðý
                Paragraph tarihBilgi = new Paragraph(
                    $"{baslangic:dd MMMM yyyy} - {bitis:dd MMMM yyyy}",
                    normalFont
                );
                tarihBilgi.Alignment = Element.ALIGN_CENTER;
                tarihBilgi.SpacingAfter = 20f;
                document.Add(tarihBilgi);

                // Çizgi
                LineSeparator line = new LineSeparator(1f, 100f, new BaseColor(211, 211, 211), Element.ALIGN_CENTER, -1);
                document.Add(new Chunk(line));
                document.Add(new Paragraph(" ", normalFont));

                // ÖZET ÝSTATÝSTÝKLER TABLOSU
                PdfPTable ozet = new PdfPTable(2);
                ozet.WidthPercentage = 100;
                ozet.SpacingBefore = 15f;
                ozet.SpacingAfter = 20f;
                ozet.DefaultCell.Border = Rectangle.NO_BORDER;
                ozet.DefaultCell.Padding = 10f;

                // Baþlýk
                PdfPCell headerCell = new PdfPCell(new Phrase("ÖZET ÝSTATÝSTÝKLER", altBaslikFont));
                headerCell.Colspan = 2;
                headerCell.BackgroundColor = new BaseColor(79, 70, 229); // Indigo
                headerCell.Border = Rectangle.NO_BORDER;
                headerCell.Padding = 12f;
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                ozet.AddCell(headerCell);

                // Toplam Ciro
                AddStatCell(ozet, "Toplam Ciro:", toplamCiro.ToString("C2"), kalinFont, normalFont, new BaseColor(59, 130, 246));
                
                // Toplam Gider
                AddStatCell(ozet, "Toplam Gider:", toplamGider.ToString("C2"), kalinFont, normalFont, new BaseColor(239, 68, 68));
                
                // Net Kâr
                BaseColor karRengi = netKar >= 0 ? new BaseColor(16, 185, 129) : new BaseColor(239, 68, 68);
                AddStatCell(ozet, "Net Kâr:", netKar.ToString("C2"), kalinFont, normalFont, karRengi);
                
                // Sipariþ Sayýsý
                AddStatCell(ozet, "Toplam Sipariþ:", $"{siparisSayisi} adet", kalinFont, normalFont, new BaseColor(251, 146, 60));
                
                // Ortalama Sipariþ
                AddStatCell(ozet, "Ortalama Sipariþ:", ortalamaSiparisDegeri.ToString("C2"), kalinFont, normalFont, new BaseColor(168, 85, 247));
                
                // Kâr Marjý
                decimal karMarji = toplamCiro > 0 ? (netKar / toplamCiro) * 100 : 0;
                AddStatCell(ozet, "Kâr Marjý:", $"%{karMarji:F2}", kalinFont, normalFont, karRengi);
                
                // Ýptal Oraný
                AddStatCell(ozet, "Ýptal Edilen Sipariþ:", $"{iptalEdilenSiparisSayisi} adet", kalinFont, normalFont, new BaseColor(239, 68, 68));
                
                // Ýptal Oraný Yüzde
                AddStatCell(ozet, "Ýptal Oraný:", $"%{iptalOrani:F2}", kalinFont, normalFont, new BaseColor(245, 158, 11));

                document.Add(ozet);

                // EN ÇOK SATAN ÜRÜNLER
                if (enCokSatanlar != null && enCokSatanlar.Any())
                {
                    document.Add(new Paragraph(" ", normalFont));
                    document.Add(new Chunk(line));
                    
                    Paragraph urunBaslik = new Paragraph("EN ÇOK SATAN ÜRÜNLER (TOP 10)", altBaslikFont);
                    urunBaslik.SpacingBefore = 15f;
                    urunBaslik.SpacingAfter = 10f;
                    document.Add(urunBaslik);

                    PdfPTable urunTable = new PdfPTable(3);
                    urunTable.WidthPercentage = 100;
                    urunTable.SetWidths(new float[] { 1f, 4f, 2f });
                    urunTable.SpacingAfter = 20f;

                    // Tablo Baþlýklarý
                    AddTableHeader(urunTable, "Sýra", kalinFont);
                    AddTableHeader(urunTable, "Ürün Adý", kalinFont);
                    AddTableHeader(urunTable, "Satýþ Adedi", kalinFont);

                    int sira = 1;
                    foreach (var urun in enCokSatanlar.Take(10))
                    {
                        AddTableCell(urunTable, $"#{sira}", normalFont, Element.ALIGN_CENTER);
                        AddTableCell(urunTable, urun.UrunAdi, normalFont);
                        AddTableCell(urunTable, $"{urun.Adet} adet", kalinFont, Element.ALIGN_RIGHT);
                        sira++;
                    }

                    document.Add(urunTable);
                }

                // GÝDER DAÐILIMI
                if (giderDagilimi != null && giderDagilimi.Any())
                {
                    document.Add(new Paragraph(" ", normalFont));
                    document.Add(new Chunk(line));
                    
                    Paragraph giderBaslik = new Paragraph("GÝDER DAÐILIMI", altBaslikFont);
                    giderBaslik.SpacingBefore = 15f;
                    giderBaslik.SpacingAfter = 10f;
                    document.Add(giderBaslik);

                    PdfPTable giderTable = new PdfPTable(3);
                    giderTable.WidthPercentage = 100;
                    giderTable.SetWidths(new float[] { 4f, 2f, 2f });
                    giderTable.SpacingAfter = 20f;

                    // Tablo Baþlýklarý
                    AddTableHeader(giderTable, "Kategori", kalinFont);
                    AddTableHeader(giderTable, "Tutar", kalinFont);
                    AddTableHeader(giderTable, "Oran", kalinFont);

                    decimal toplamGiderHesap = giderDagilimi.Sum(g => g.Tutar);
                    foreach (var gider in giderDagilimi)
                    {
                        decimal oran = toplamGiderHesap > 0 ? (gider.Tutar / toplamGiderHesap) * 100 : 0;
                        AddTableCell(giderTable, gider.Kategori, normalFont);
                        AddTableCell(giderTable, gider.Tutar.ToString("C2"), normalFont, Element.ALIGN_RIGHT);
                        AddTableCell(giderTable, $"%{oran:F1}", kalinFont, Element.ALIGN_RIGHT);
                    }

                    document.Add(giderTable);
                }

                // PERSONEL PERFORMANSI
                if (personelPerformansi != null && personelPerformansi.Any())
                {
                    document.Add(new Paragraph(" ", normalFont));
                    document.Add(new Chunk(line));
                    
                    Paragraph personelBaslik = new Paragraph("PERSONEL PERFORMANSI (TOP 5)", altBaslikFont);
                    personelBaslik.SpacingBefore = 15f;
                    personelBaslik.SpacingAfter = 10f;
                    document.Add(personelBaslik);

                    PdfPTable personelTable = new PdfPTable(4);
                    personelTable.WidthPercentage = 100;
                    personelTable.SetWidths(new float[] { 1f, 3f, 2f, 2f });
                    personelTable.SpacingAfter = 20f;

                    // Tablo Baþlýklarý
                    AddTableHeader(personelTable, "Sýra", kalinFont);
                    AddTableHeader(personelTable, "Personel Adý", kalinFont);
                    AddTableHeader(personelTable, "Sipariþ", kalinFont);
                    AddTableHeader(personelTable, "Toplam Ciro", kalinFont);

                    int siraPers = 1;
                    foreach (var pers in personelPerformansi.Take(5))
                    {
                        AddTableCell(personelTable, $"#{siraPers}", normalFont, Element.ALIGN_CENTER);
                        AddTableCell(personelTable, pers.PersonelAdi, normalFont);
                        AddTableCell(personelTable, $"{pers.SiparisSayisi} adet", normalFont, Element.ALIGN_CENTER);
                        AddTableCell(personelTable, pers.ToplamCiro.ToString("C2"), kalinFont, Element.ALIGN_RIGHT);
                        siraPers++;
                    }

                    document.Add(personelTable);
                }

                // KATEGORÝ SATIÞLARI
                if (kategoriSatislari != null && kategoriSatislari.Any())
                {
                    document.Add(new Paragraph(" ", normalFont));
                    document.Add(new Chunk(line));
                    
                    Paragraph kategoriBaslik = new Paragraph("KATEGORÝ BAZLI SATIÞLAR", altBaslikFont);
                    kategoriBaslik.SpacingBefore = 15f;
                    kategoriBaslik.SpacingAfter = 10f;
                    document.Add(kategoriBaslik);

                    PdfPTable kategoriTable = new PdfPTable(3);
                    kategoriTable.WidthPercentage = 100;
                    kategoriTable.SetWidths(new float[] { 4f, 2f, 2f });
                    kategoriTable.SpacingAfter = 20f;

                    // Tablo Baþlýklarý
                    AddTableHeader(kategoriTable, "Kategori", kalinFont);
                    AddTableHeader(kategoriTable, "Satýþ Tutarý", kalinFont);
                    AddTableHeader(kategoriTable, "Oran", kalinFont);

                    decimal toplamKategoriSatis = kategoriSatislari.Sum(k => k.ToplamSatis);
                    foreach (var kat in kategoriSatislari)
                    {
                        decimal oran = toplamKategoriSatis > 0 ? (kat.ToplamSatis / toplamKategoriSatis) * 100 : 0;
                        AddTableCell(kategoriTable, kat.Kategori, normalFont);
                        AddTableCell(kategoriTable, kat.ToplamSatis.ToString("C2"), normalFont, Element.ALIGN_RIGHT);
                        AddTableCell(kategoriTable, $"%{oran:F1}", kalinFont, Element.ALIGN_RIGHT);
                    }

                    document.Add(kategoriTable);
                }

                // ÖDEME YÖNTEMLERÝ DAÐILIMI
                if (odemeYontemleri != null && odemeYontemleri.Any())
                {
                    document.Add(new Paragraph(" ", normalFont));
                    document.Add(new Chunk(line));
                    
                    Paragraph odemeBaslik = new Paragraph("ÖDEME YÖNTEMLERÝ DAÐILIMI", altBaslikFont);
                    odemeBaslik.SpacingBefore = 15f;
                    odemeBaslik.SpacingAfter = 10f;
                    document.Add(odemeBaslik);

                    PdfPTable odemeTable = new PdfPTable(4);
                    odemeTable.WidthPercentage = 100;
                    odemeTable.SetWidths(new float[] { 3f, 2f, 2f, 2f });
                    odemeTable.SpacingAfter = 20f;

                    // Tablo Baþlýklarý
                    AddTableHeader(odemeTable, "Ödeme Yöntemi", kalinFont);
                    AddTableHeader(odemeTable, "Ýþlem Sayýsý", kalinFont);
                    AddTableHeader(odemeTable, "Tutar", kalinFont);
                    AddTableHeader(odemeTable, "Oran", kalinFont);

                    int toplamOdemeAdet = odemeYontemleri.Sum(o => o.Adet);
                    foreach (var odeme in odemeYontemleri)
                    {
                        double oran = toplamOdemeAdet > 0 ? ((double)odeme.Adet / toplamOdemeAdet) * 100 : 0;
                        AddTableCell(odemeTable, odeme.OdemeTuruAdi, normalFont);
                        AddTableCell(odemeTable, $"{odeme.Adet} adet", normalFont, Element.ALIGN_CENTER);
                        AddTableCell(odemeTable, odeme.Tutar.ToString("C2"), normalFont, Element.ALIGN_RIGHT);
                        AddTableCell(odemeTable, $"%{oran:F1}", kalinFont, Element.ALIGN_RIGHT);
                    }

                    document.Add(odemeTable);
                }

                // EK METRIKLER
                document.Add(new Paragraph(" ", normalFont));
                document.Add(new Chunk(line));
                
                Paragraph metrikBaslik = new Paragraph("EK PERFORMANS METRÝKLERÝ", altBaslikFont);
                metrikBaslik.SpacingBefore = 15f;
                metrikBaslik.SpacingAfter = 10f;
                document.Add(metrikBaslik);

                PdfPTable metrikTable = new PdfPTable(2);
                metrikTable.WidthPercentage = 100;
                metrikTable.SpacingAfter = 20f;
                metrikTable.DefaultCell.Border = Rectangle.NO_BORDER;
                metrikTable.DefaultCell.Padding = 8f;

                // Gider/Ciro Oraný
                decimal giderOrani = toplamCiro > 0 ? (toplamGider / toplamCiro) * 100 : 0;
                AddStatCell(metrikTable, "Gider/Ciro Oraný:", $"%{giderOrani:F2}", kalinFont, normalFont, new BaseColor(239, 68, 68));
                
                // En Yüksek Ciro Günü
                if (enYuksekGun.HasValue)
                {
                    string enYuksekGunText = $"{enYuksekGun.Value.Tutar:C2} ({enYuksekGun.Value.Tarih:dd.MM.yyyy})";
                    AddStatCell(metrikTable, "Rekor Gün:", enYuksekGunText, kalinFont, normalFont, new BaseColor(16, 185, 129));
                }
                
                // Dönem Bilgisi
                int gunSayisi = (bitis - baslangic).Days + 1;
                decimal gunlukOrtalamaCiro = gunSayisi > 0 ? toplamCiro / gunSayisi : 0;
                AddStatCell(metrikTable, "Günlük Ort. Ciro:", gunlukOrtalamaCiro.ToString("C2"), kalinFont, normalFont, new BaseColor(59, 130, 246));
                
                // Günlük Ortalama Sipariþ
                decimal gunlukOrtalamaSiparis = gunSayisi > 0 ? (decimal)siparisSayisi / gunSayisi : 0;
                AddStatCell(metrikTable, "Günlük Ort. Sipariþ:", $"{gunlukOrtalamaSiparis:F1} adet", kalinFont, normalFont, new BaseColor(168, 85, 247));

                document.Add(metrikTable);

                // FOOTER
                document.Add(new Paragraph(" ", normalFont));
                document.Add(new Chunk(line));
                
                Paragraph footer = new Paragraph(
                    $"Rapor Tarihi: {DateTime.Now:dd MMMM yyyy HH:mm}\n" +
                    "Restoran Yönetim Sistemi\n" +
                    "© 2025 Tüm Haklarý Saklýdýr",
                    kucukFont
                );
                footer.Alignment = Element.ALIGN_CENTER;
                footer.SpacingBefore = 20f;
                document.Add(footer);

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Helper Metodlar
        private static void AddStatCell(PdfPTable table, string label, string value, Font labelFont, Font valueFont, BaseColor renk)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 8f;
            labelCell.BackgroundColor = new BaseColor(248, 250, 252);
            table.AddCell(labelCell);

            PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.Padding = 8f;
            valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            valueCell.BackgroundColor = new BaseColor(renk.R, renk.G, renk.B, 20); // %8 opacity
            table.AddCell(valueCell);
        }

        private static void AddTableHeader(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = new BaseColor(241, 245, 249);
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColor = new BaseColor(211, 211, 211);
            cell.Padding = 10f;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        private static void AddTableCell(PdfPTable table, string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColor = new BaseColor(241, 245, 249);
            cell.Padding = 10f;
            cell.HorizontalAlignment = alignment;
            table.AddCell(cell);
        }
    }
}

