using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;

namespace TelefonRehberi.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext _context;

        // Dependency Injection ile MyDbContext'i controller'a alýyoruz
        public HomeController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            // Eðer kullanýcý zaten giriþ yaptýysa, Login sayfasýna gitmesine izin verme ve ana sayfaya yönlendir
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Contacts", "Home"); // Ana sayfaya yönlendir (ya da baþka bir sayfa)
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string loginInput, string password, string rememberMe)
        {
            bool remember = !rememberMe.IsNullOrEmpty();
            
            // Kullanýcýyý hem username hem de email üzerinden sorgula
            var user = await _context.Users
                                      .Where(u => u.Username == loginInput || u.Email == loginInput)
                                      .FirstOrDefaultAsync();

            if (user != null)
            {
                if (user.Password == password)
                {
                    // Kullanýcýyý doðruladýktan sonra Claims oluþturuyoruz
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = remember, // Eðer Remember Me iþaretliyse, cookie sürekli olacak
                        ExpiresUtc = remember ? DateTime.UtcNow.AddDays(30) : (DateTime?)null // 30 gün süreyle geçerli olacak cookie
                    };
                    // Kullanýcýyý oturum açtýr
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Contacts", "Home");
                }
            }

            // Giriþ baþarýsýz ise hata mesajý göster
            ViewBag.ErrorMessage = "Geçersiz kullanýcý adý, email veya þifre.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Kullanýcýyý çýkart
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Contacts()
        {
            // Veritabanýndan kayýtlarý asenkron olarak çekiyoruz
            var contacts = await _context.Contacts.ToListAsync();
            return View(contacts); // View'a verileri gönderiyoruz
        }

        [HttpPost]
        public async Task<IActionResult> AddContact(Contact contact)
        {


            // Contact modelini doldur
            contact.CreatedBy = User.Identity.Name;
            contact.CreatedAt = DateTime.Now;
            contact.IsDeleted = false;


            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction("Contacts", "Home");

        }

        [HttpPost]
        public IActionResult EditContact([FromBody] Contact updatedContact)
        {
            if (updatedContact == null || updatedContact.ContactId < 0)
            {
                return BadRequest(new { message = "Geçersiz veri." });
            }

            try
            {
                // Veritabanýnda ilgili kaydý bul ve güncelle
                var contact = _context.Contacts.Find(updatedContact.ContactId);
                if (contact == null)
                {
                    return NotFound(new { message = "Kayýt bulunamadý." });
                }

                contact.Name = updatedContact.Name;
                contact.Surname = updatedContact.Surname;
                contact.PhoneNumber = updatedContact.PhoneNumber;

                _context.SaveChanges();

                return RedirectToAction("Contacts", "Home");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bir hata oluþtu.", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteContacts([FromBody] List<int> contactIds)
        {
            if (contactIds == null || !contactIds.Any())
            {
                return BadRequest(new { message = "Hiçbir öðe seçilmedi." });
            }

            try
            {
                // Veritabanýndan seçilen ContactId'leri güncelle
                foreach (var id in contactIds)
                {
                    var contact = _context.Contacts.Find(id);
                    if (contact != null)
                    {
                        contact.IsDeleted = true;  // Soft delete için isDeleted alanýný true yapýyoruz
                        _context.Contacts.Update(contact);  // Güncellenen veriyi kaydediyoruz
                    }
                }

                // Deðiþiklikleri kaydet
                _context.SaveChanges();

                return RedirectToAction("Contacts", "Home");
            }
            catch (Exception ex)
            {
                // Hata durumunda kullanýcýya bilgi ver
                return StatusCode(500, new { message = "Bir hata oluþtu.", error = ex.Message });
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
