using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using PortalDMPlace.Functions;
using Microsoft.AspNetCore.Authorization;

namespace PortalDMPlace.Controllers
{
    [Authorize]
    public class AccountController(DataContext context, AccountObject user, IWebHostEnvironment environment) : Controller
    {
        private readonly DataContext _context = context;
        private readonly AccountObject _user = user;
        private readonly IWebHostEnvironment _environment = environment;

        [HttpGet("MeuPerfil")]
        public async Task<IActionResult> Index()
        {
            // O [Authorize] já garante a autenticação, mas buscamos os dados frescos do banco
            var dbUser = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == _user.Id);
            if (dbUser == null) return RedirectToAction("Logout", "Admin");

            return View(dbUser);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage(IFormFile profileImage)
        {
            if (profileImage == null || profileImage.Length == 0) return RedirectToAction("Index");

            // Validação de Extensão (Segurança Arcanista)
            var supportedTypes = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(profileImage.FileName).ToLower();
            if (!supportedTypes.Contains(extension))
            {
                TempData["Erro"] = "Formato de imagem não suportado. Use JPG, PNG ou WebP.";
                return RedirectToAction("Index");
            }

            var user = await _context.Accounts.FindAsync(_user.Id);
            if (user == null) return NotFound();

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "img/profiles");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // --- MELHORIA: Apagar a imagem antiga se não for a default ---
            if (!string.IsNullOrEmpty(user.ProfileImageUrl) && !user.ProfileImageUrl.Contains("default.png"))
            {
                string oldPath = Path.Combine(_environment.WebRootPath, user.ProfileImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"profile_{user.Id}_{DateTime.Now.Ticks}{extension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(fileStream);
            }

            user.ProfileImageUrl = "/img/profiles/" + fileName;
            await _context.SaveChangesAsync();
            
            _user.ProfileImageUrl = user.ProfileImageUrl; 

            TempData["Sucesso"] = "Sua nova face foi revelada ao mundo!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["Erro"] = "A nova senha deve ter pelo menos 6 caracteres.";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Erro"] = "As senhas novas não coincidem.";
                return RedirectToAction("Index");
            }

            var user = await _context.Accounts.FindAsync(_user.Id);
            if (user == null) return RedirectToAction("Logout", "Admin");
            
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.HashPassword))
            {
                TempData["Erro"] = "A senha atual está incorreta. O ritual de troca falhou!";
                return RedirectToAction("Index");
            }

            user.HashPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Sua alma foi renovada! Senha alterada com sucesso.";
            return RedirectToAction("Index");
        }
    }
}