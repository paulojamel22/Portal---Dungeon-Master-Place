using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace PortalDMPlace.Controllers
{
    public class AdminController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        // --- AUTHENTICATION (Login / Logout) ---

        [HttpGet]
        public IActionResult Login() 
        {
            if (User.Identity?.IsAuthenticated == true) 
                return RedirectToAction("Index");
                
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Busca o usuário de forma assíncrona
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            {
                ViewBag.Erro = "As sombras escondem sua identidade (Usuário ou senha inválidos).";
                return View();
            }

            // 1. Criar as Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Username),
                new("CampanhaId", user.CampanhaId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. SignIn no ASP.NET
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                new AuthenticationProperties { 
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Sessão de 7 dias
                });

            // Sincroniza a Session para uso em Helpers legados
            HttpContext.Session.SetInt32("Campanha", user.CampanhaId);
            HttpContext.Session.SetString("Usuario", user.Username);

            Console.WriteLine($"[LOGIN SUCCESS] Mestre {username} acessou o trono.");

            return RedirectToAction("Index", "Admin");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // --- DASHBOARD PRINCIPAL ---

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Dados para a tabela de Crônicas Recentes
            var noticiasRecentes = await _context.Noticias
                .Include(n => n.Campanha)
                .OrderByDescending(n => n.DataPublicacao)
                .Take(5)
                .ToListAsync();

            // Estatísticas rápidas (Contagem paralela para performance)
            ViewBag.TotalCampanhas = await _context.Campanhas.CountAsync();
            ViewBag.TotalNoticias = await _context.Noticias.CountAsync();
            
            return View(noticiasRecentes);
        }

        // --- REGISTRO DE MESTRES ---
        // (Recomendo deixar comentado ou protegido após criar seu usuário principal)
        [HttpGet]
        public IActionResult Registrar() => View();

        [HttpPost]
        public async Task<IActionResult> Registrar(Account model)
        {
            if (await _context.Accounts.AnyAsync(u => u.Username == model.Username))
            {
                ViewBag.Erro = "Este nome já consta nos registros antigos.";
                return View();
            }

            model.HashPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
            model.Password = string.Empty;

            _context.Accounts.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }
    }
}