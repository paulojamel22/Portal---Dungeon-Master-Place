using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using PortalDMPlace.Functions;
using PortalDMPlace.Models.ViewModels;
using PortalDMPlace.Enumerators;

namespace PortalDMPlace.Controllers
{
    public class AdminController(DataContext context, AccountObject accountObject) : Controller
    {
        private readonly DataContext _context = context;
        private readonly AccountObject _accountObject = accountObject;

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
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            {
                ViewBag.Erro = "As sombras escondem sua identidade (Usuário ou senha inválidos).";
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new("Username", user.Username),
                new("CampanhaId", user.CampanhaId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                new AuthenticationProperties { 
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) 
                });

            // Sincroniza a Session para uso em Helpers legados
            HttpContext.Session.SetInt32("CampanhaId", user.CampanhaId);
            HttpContext.Session.SetString("Username", user.Username);

            // Sincroniza o objeto de sessão atual
            _accountObject.LoadUserData(user);

            return RedirectToAction("Index", "Admin");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _accountObject.UnloadUserData();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var noticiasQuery = _context.Noticias.Include(n => n.Campanha).AsQueryable();
            var campanhasQuery = _context.Campanhas.AsQueryable();

            // SOBERANIA DE DADOS: 
            // Se não for Admin/Dev, filtra para mostrar apenas o que pertence ao usuário logado
            if (!_accountObject.IsAtLeastAdmin)
            {
                noticiasQuery = noticiasQuery.Where(n => n.AccountId == _accountObject.Id);
                campanhasQuery = campanhasQuery.Where(c => c.CriadorId == _accountObject.Id);
            }

            ViewBag.TotalCampanhas = await campanhasQuery.CountAsync();
            ViewBag.TotalNoticias = await noticiasQuery.CountAsync();

            var ultimasNoticias = await noticiasQuery
                .OrderByDescending(n => n.DataPublicacao)
                .Take(5) 
                .ToListAsync();

            return View(ultimasNoticias);
        }

        [HttpGet]
        public async Task<IActionResult> Register() 
        {
            // Carrega as campanhas para o dropdown de registro
            ViewBag.Campanhas = await _context.Campanhas.AsNoTracking().ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) 
            {
                ViewBag.Campanhas = await _context.Campanhas.AsNoTracking().ToListAsync();
                return View(model);
            }

            // Impede que usuários se registrem como Admin/Dev manualmente
            if (model.AccountType == AccountType.Administrator || model.AccountType == AccountType.Developer)
            {
                ModelState.AddModelError("AccountType", "Acesso restrito. Somente os deuses concedem este poder.");
                ViewBag.Campanhas = await _context.Campanhas.AsNoTracking().ToListAsync();
                return View(model);
            }

            // Verifica se o usuário já existe para evitar erro de constraint no SQLite
            if (await _context.Accounts.AnyAsync(a => a.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Este nome de usuário já foi invocado.");
                ViewBag.Campanhas = await _context.Campanhas.AsNoTracking().ToListAsync();
                return View(model);
            }

            var newAccount = new Account
            {
                Name = model.Name,
                Username = model.Username,
                // Email = model.Email, // Descomente se o campo Email estiver no Account
                HashPassword = BCrypt.Net.BCrypt.HashPassword(model.Password),
                AccountType = model.AccountType,
                CreatedAt = DateTime.Now,
                ProfileImageUrl = "/img/profiles/default.png",
                CampanhaId = model.CampanhaId // Vincula à campanha escolhida no registro
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();
            
            TempData["Sucesso"] = "Sua identidade foi forjada! Faça seu login.";
            return RedirectToAction("Login");
        }
    }
}