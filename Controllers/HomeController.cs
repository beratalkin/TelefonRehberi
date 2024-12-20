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

        // Dependency Injection ile MyDbContext'i controller'a al�yoruz
        public HomeController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            // E�er kullan�c� zaten giri� yapt�ysa, Login sayfas�na gitmesine izin verme ve ana sayfaya y�nlendir
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Contacts", "Home"); // Ana sayfaya y�nlendir (ya da ba�ka bir sayfa)
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string loginInput, string password, string rememberMe)
        {
            bool remember = !rememberMe.IsNullOrEmpty();
            
            // Kullan�c�y� hem username hem de email �zerinden sorgula
            var user = await _context.Users
                                      .Where(u => u.Username == loginInput || u.Email == loginInput)
                                      .FirstOrDefaultAsync();

            if (user != null)
            {
                if (user.Password == password)
                {
                    // Kullan�c�y� do�rulad�ktan sonra Claims olu�turuyoruz
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = remember, // E�er Remember Me i�aretliyse, cookie s�rekli olacak
                        ExpiresUtc = remember ? DateTime.UtcNow.AddDays(30) : (DateTime?)null // 30 g�n s�reyle ge�erli olacak cookie
                    };
                    // Kullan�c�y� oturum a�t�r
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Contacts", "Home");
                }
            }

            // Giri� ba�ar�s�z ise hata mesaj� g�ster
            ViewBag.ErrorMessage = "Ge�ersiz kullan�c� ad�, email veya �ifre.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Kullan�c�y� ��kart
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Contacts()
        {
            // Veritaban�ndan kay�tlar� asenkron olarak �ekiyoruz
            var contacts = await _context.Contacts.ToListAsync();
            return View(contacts); // View'a verileri g�nderiyoruz
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
                return BadRequest(new { message = "Ge�ersiz veri." });
            }

            try
            {
                // Veritaban�nda ilgili kayd� bul ve g�ncelle
                var contact = _context.Contacts.Find(updatedContact.ContactId);
                if (contact == null)
                {
                    return NotFound(new { message = "Kay�t bulunamad�." });
                }

                contact.Name = updatedContact.Name;
                contact.Surname = updatedContact.Surname;
                contact.PhoneNumber = updatedContact.PhoneNumber;

                _context.SaveChanges();

                return RedirectToAction("Contacts", "Home");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bir hata olu�tu.", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteContacts([FromBody] List<int> contactIds)
        {
            if (contactIds == null || !contactIds.Any())
            {
                return BadRequest(new { message = "Hi�bir ��e se�ilmedi." });
            }

            try
            {
                // Veritaban�ndan se�ilen ContactId'leri g�ncelle
                foreach (var id in contactIds)
                {
                    var contact = _context.Contacts.Find(id);
                    if (contact != null)
                    {
                        contact.IsDeleted = true;  // Soft delete i�in isDeleted alan�n� true yap�yoruz
                        _context.Contacts.Update(contact);  // G�ncellenen veriyi kaydediyoruz
                    }
                }

                // De�i�iklikleri kaydet
                _context.SaveChanges();

                return RedirectToAction("Contacts", "Home");
            }
            catch (Exception ex)
            {
                // Hata durumunda kullan�c�ya bilgi ver
                return StatusCode(500, new { message = "Bir hata olu�tu.", error = ex.Message });
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
