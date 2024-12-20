using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Veritabaný baðlantýsýný ve DbContext'i kaydetme
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // DefaultConnection connection string'inizi config'den çekiyor

// Cookie Authentication ayarlarý
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login"; // Giriþ yapýlmamýþsa bu sayfaya yönlendirilir
        options.LogoutPath = "/Home/Logout"; // Çýkýþ yolu
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Kimlik doðrulama ve yetkilendirme middleware'lerini ekle
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Ana yönlendirme
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();