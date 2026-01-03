using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;

namespace PortalDMPlace.Controllers
{
    public class DesafioController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        // ID fixo conforme sua estrutura de banco para o Desafio de Sangue
        private const int DESAFIO_CAMPANHA_ID = 2;

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            int noticiasPorPagina = 6;

            // Filtramos as notícias especificamente para a Campanha 2
            var query = _context.Noticias
                .Where(n => n.CampanhaId == DESAFIO_CAMPANHA_ID)
                .AsNoTracking(); // Otimização para leitura no servidor Ampere

            var totalNoticias = await query.CountAsync();

            var noticias = await query
                .OrderByDescending(n => n.DataPublicacao)
                .Skip((page - 1) * noticiasPorPagina)
                .Take(noticiasPorPagina)
                .ToListAsync();

            // Organização de metadados para a View
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalNoticias / (double)noticiasPorPagina);
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };

            // Busca os detalhes da campanha para títulos ou banners dinâmicos
            var campanha = await _context.Campanhas.FindAsync(DESAFIO_CAMPANHA_ID);
            ViewBag.NomeCampanha = campanha?.Name ?? "Desafio de Sangue";

            return View(noticias);
        }

        [HttpGet]
        public async Task<IActionResult> Detalhes(int id)
        {
            if (id <= 0)
                return RedirectToAction(nameof(Index));

            // Segurança: busca a notícia garantindo que ela pertence ao Desafio de Sangue
            var noticia = await _context.Noticias
                .Include(n => n.Campanha)
                .FirstOrDefaultAsync(n => n.Id == id && n.CampanhaId == DESAFIO_CAMPANHA_ID);

            if (noticia == null)
            {
                TempData["Erro"] = "Esta crônica de sangue não foi encontrada.";
                return RedirectToAction(nameof(Index));
            }

            return View(noticia);
        }
    }
}
