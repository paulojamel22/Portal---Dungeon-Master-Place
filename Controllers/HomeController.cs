using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;

namespace PortalDMPlace.Controllers
{
    public class HomeController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Usamos AsNoTracking para performance máxima na home
            // e ToListAsync para não travar a thread principal do servidor
            var campanhas = await _context.Campanhas
                .AsNoTracking()
                .ToListAsync();

            if (campanhas == null || !campanhas.Any())
            {
                // Mensagem amigável caso o banco esteja vazio (como no seu exemplo do GitHub)
                TempData["Info"] = "Bem-vindo ao Portal Dungeon Master Place. Nenhuma crônica foi iniciada ainda.";
            }

            return View(campanhas);
        }

        // Action para a página de erro padrão do ASP.NET Core
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
