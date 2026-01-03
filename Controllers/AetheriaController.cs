using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using System.Numerics;

namespace PortalDMPlace.Controllers
{
    public class AetheriaController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;
        
        // Definimos o ID da campanha Aetheria como constante para facilitar manutenção
        private const int AETHERIA_CAMPANHA_ID = 1;

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string? categoria = null)
        {
            int noticiasPorPagina = 6;

            // Iniciamos a Query filtrando pela campanha correta e incluindo os dados da Campanha se necessário
            var query = _context.Noticias
                .Where(n => n.CampanhaId == AETHERIA_CAMPANHA_ID)
                .AsNoTracking(); // Performance: NoTracking é ideal para listagens de leitura

            // Filtro por categoria (caso você queira adicionar filtros na sua View no futuro)
            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(n => n.Categoria == categoria);
            }

            var totalNoticias = await query.CountAsync();

            // Paginação otimizada
            var noticias = await query
                .OrderByDescending(n => n.DataPublicacao)
                .Skip((page - 1) * noticiasPorPagina)
                .Take(noticiasPorPagina)
                .ToListAsync();

            // Dados para a View
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalNoticias / (double)noticiasPorPagina);
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            ViewBag.CategoriaAtual = categoria;

            // Busca o nome da campanha para exibir no título da página dinamicamente
            var campanha = await _context.Campanhas.FindAsync(AETHERIA_CAMPANHA_ID);
            ViewBag.NomeCampanha = campanha?.Name ?? "Ecos de Aetheria";

            return View(noticias);
        }

        [HttpGet]
        public async Task<IActionResult> Detalhes(int id)
        {
            if (id <= 0)
                return RedirectToAction(nameof(Index));

            // Buscamos a notícia garantindo que ela pertence a esta campanha (segurança)
            var noticia = await _context.Noticias
                .Include(n => n.Campanha) // Carrega os dados da campanha se precisar usar na View
                .FirstOrDefaultAsync(n => n.Id == id && n.CampanhaId == AETHERIA_CAMPANHA_ID);

            if (noticia == null)
            {
                TempData["Erro"] = "A crônica solicitada não foi encontrada nos registros de Aetheria.";
                return RedirectToAction(nameof(Index));
            }

            return View(noticia);
        }
    }
}