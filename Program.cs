using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Veritaban� ba�lant�s�n� ve DbContext'i kaydetme
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // DefaultConnection connection string'inizi config'den �ekiyor

// Cookie Authentication ayarlar�
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login"; // Giri� yap�lmam��sa bu sayfaya y�nlendirilir
        options.LogoutPath = "/Home/Logout"; // ��k�� yolu
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

// Kimlik do�rulama ve yetkilendirme middleware'lerini ekle
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Ana y�nlendirme
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();