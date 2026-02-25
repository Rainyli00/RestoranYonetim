using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoranYonetim.Helpers;
using RestoranYonetim.Models;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ===== KÜLTÜR AYARLARI (Decimal Separator için) =====
// Gösterim için tr-TR, ama parsing için özel ModelBinder kullanýyoruz
var cultureInfo = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 1. MVC SERVÝSLERÝ (+ Canlý Yenileme Özelliði + Özel Model Binder)
// Bu sayede HTML deðiþtirince projeyi kapatýp açmana gerek kalmaz.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddControllersWithViews(options =>
    {
        // Decimal parsing için özel Model Binder ekle (hem nokta hem virgül kabul eder)
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
    }).AddRazorRuntimeCompilation();
}
else
{
    builder.Services.AddControllersWithViews(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
    });
}

// 2. DB CONTEXT (VERÝTABANI BAÐLANTISI)
builder.Services.AddDbContext<RestoranDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. SESSION (OTURUM) SERVÝSLERÝ
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // 60 dk hareketsiz kalýrsa çýkýþ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. HTTP CLIENT FACTORY (AI Servisi için)
builder.Services.AddHttpClient();

var app = builder.Build();

// --- AYARLAR (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. SESSION'I AKTÝF ET (Sýralama Doðru ?)
app.UseSession();

app.UseAuthorization();

// 5. ROTA AYARI
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); // Tek sefer yazýlýr, uygulama burada baþlar.