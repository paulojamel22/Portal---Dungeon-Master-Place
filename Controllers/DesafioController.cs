using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;

namespace PortalDMPlace.Controllers
{
    public class DesafioController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        public async Task<IActionResult> Index(int page = 1)
        {
            int NoticiasPorPagina = 6;

            var noticias = _context.Noticias
                .Where(n => n.Campanha == 2)
                .OrderByDescending(n => n.DataPublicacao);

            var TotalNoticia = await noticias.CountAsync();

            var GetPage = await noticias.Skip((page - 1) * NoticiasPorPagina).Take(NoticiasPorPagina).ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(TotalNoticia / (double)NoticiasPorPagina);
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };

            return View(GetPage);
        }

        public async Task<IActionResult> Detalhes(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Desafio");
            }

            var noticia = await _context.Noticias.FirstOrDefaultAsync(n => n.Id == id);

            if (noticia == null)
            {
                TempData["Erro"] = "Notícia não encontrada.";
                return RedirectToAction("Index", "Desafio");
            }

            return View(noticia);
        }
    }
}
