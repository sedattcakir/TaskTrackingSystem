using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTrackingSystem.Data;
using TaskTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;

namespace TaskTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
        [EnableRateLimiting("LoginLimit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            string? userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var captchaResponse = Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrEmpty(captchaResponse))
            {
                ModelState.AddModelError(string.Empty, "Lütfen robot olmadığınızı doğrulayın.");
                return View(dto);
            }

            var secretKey = _configuration["Recaptcha:SecretKey"];
            using var httpClient = new HttpClient();
            var captchaResult = await httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaResponse}", null);
            var captchaJson = await captchaResult.Content.ReadAsStringAsync();

            if (!captchaJson.Contains("\"success\": true"))
            {
                ModelState.AddModelError(string.Empty, "CAPTCHA doğrulaması başarısız.");
                return View(dto);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                ModelState.AddModelError(string.Empty, "Email ve şifre zorunludur.");

                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email ?? "Bilinmiyor",
                    Action = "Başarısız login",
                    IpAddress = userIp
                });
                await _context.SaveChangesAsync();

                return View(dto);
            }

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                BCrypt.Net.BCrypt.Verify("dummy", "$2a$11$aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                ModelState.AddModelError(string.Empty, "Yanlış email veya şifre.");

                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email,
                    Action = "Başarısız login",
                    IpAddress = userIp
                });
                await _context.SaveChangesAsync();

                return View(dto);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Yanlış email veya şifre.");

                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email,
                    Action = "Başarısız login",
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
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(120);
                    user.FailedAttempts = 0;
                }
                await _context.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "Yanlış email veya şifre.");

                _context.AuditLogs.Add(new AuditLog
                {
                    UserEmail = dto.Email,
                    Action = "Başarısız login",
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

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            if (user.MustChangePassword)
            {
                return RedirectToAction("ChangePassword", "Account");
            }

            return RedirectToAction("Index", "Tasks");
        }

        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet("/Account/ChangePassword")]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                string.IsNullOrWhiteSpace(dto.NewPassword) ||
                string.IsNullOrWhiteSpace(dto.ConfirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Tüm alanları doldurunuz.");
                return View(dto);
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Yeni şifreler eşleşmiyor.");
                return View(dto);
            }

            if (dto.NewPassword.Length < 8)
            {
                ModelState.AddModelError(string.Empty, "Şifre en az 8 karakter olmalı.");
                return View(dto);
            }

            if (!dto.NewPassword.Any(char.IsUpper))
            {
                ModelState.AddModelError(string.Empty, "Şifre en az 1 büyük harf içermeli.");
                return View(dto);
            }

            if (!dto.NewPassword.Any(char.IsDigit))
            {
                ModelState.AddModelError(string.Empty, "Şifre en az 1 rakam içermeli.");
                return View(dto);
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Mevcut şifre yanlış.");
                return View(dto);
            }

            if (dto.CurrentPassword == dto.NewPassword)
            {
                ModelState.AddModelError(string.Empty, "Yeni şifre eski şifre ile aynı olamaz.");
                return View(dto);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            string? userIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            _context.AuditLogs.Add(new AuditLog
            {
                UserEmail = user.Email,
                Action = "Şifre değiştirildi",
                IpAddress = userIp
            });
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Tasks");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            ViewBag.Message = "Eğer bu email kayıtlıysa, şifre sıfırlama bağlantısı gönderildi.";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.PasswordResetToken = Guid.NewGuid().ToString();
                user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Reset link: /Account/ResetPassword?token={user.PasswordResetToken}");
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            ViewBag.Token = token;
            return View();
        }

        [HttpPost("/Account/ResetPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordPost()
        {
            var token = Request.Form["Token"].ToString();
            var newPassword = Request.Form["NewPassword"].ToString();
            var confirmPassword = Request.Form["ConfirmPassword"].ToString();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Şifreler eşleşmiyor.");
                ViewBag.Token = token;
                return View("ResetPassword");
            }

            var passwordError = ValidatePassword(newPassword);
            if (passwordError != null)
            {
                ModelState.AddModelError(string.Empty, passwordError);
                ViewBag.Token = token;
                return View("ResetPassword");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz veya süresi dolmuş bağlantı.");
                return View("ResetPassword");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        public static string? ValidatePassword(string password)
        {
            if (password.Length < 8)
                return "Şifre en az 8 karakter olmalı.";
            if (!password.Any(char.IsUpper))
                return "Şifre en az 1 büyük harf içermeli.";
            if (!password.Any(char.IsLower))
                return "Şifre en az 1 küçük harf içermeli.";
            if (!password.Any(char.IsDigit))
                return "Şifre en az 1 rakam içermeli.";
            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:',.<>?/~`".Contains(c)))
                return "Şifre en az 1 özel karakter içermeli (!@#$%^&* vb.).";

            return null;
        }
    }
}
