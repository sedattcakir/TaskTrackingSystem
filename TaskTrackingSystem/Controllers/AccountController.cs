using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTrackingSystem.Data;
using TaskTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace TaskTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Tasks");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            string? userIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                ModelState.AddModelError(string.Empty, "Email ve şifre zorunludur..");

                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email ?? "Bilinmiyor",
                    Action = "Başarısız login (boş alan)",
                    IpAddress = userIp
                });
                await _context.SaveChangesAsync();

                return View(dto);
            }

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Yanlış email veya şifre.");

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email,
                    Action = "Başarısız login (kullanıcı yok)",
                    IpAddress = userIp
                });
                await _context.SaveChangesAsync();

                return View(dto);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, $"Hesap kilitli. {user.LockoutEnd.Value.ToLocalTime()} tarihine kadar bekleyin.");

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email,
                    Action = "Başarısız login (hesap kilitli)",
                    IpAddress = userIp
                });
                await _context.SaveChangesAsync();

                return View(dto);
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                    user.FailedAttempts = 0;
                }
                await _context.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "Yanlış email veya şifre.");

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email,
                    Action = "Başarısız login (yanlış şifre)",
                    IpAddress = userIp
                });
                await _context.SaveChangesAsync();

                return View(dto);
            }

            
            user.FailedAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();


            _context.AuditLogs.Add(new AuditLog
            {
                UserEmail = dto.Email,
                Action = "Başarılı login",
                IpAddress = userIp
            });
            await _context.SaveChangesAsync();


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Index", "Tasks");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        
    }

}
